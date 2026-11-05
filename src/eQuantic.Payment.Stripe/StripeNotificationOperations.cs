using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Stripe.V1;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe;

/// <summary>
/// Verifies Stripe's signature on an event, then parses it. <c>Stripe-Signature</c> carries a timestamp
/// (<c>t</c>) and an HMAC-SHA256 (<c>v1</c>) of <c>{t}.{raw body}</c> under the endpoint's signing secret,
/// several of them while a secret is being rolled; any one is enough. A timestamp further from now than the
/// tolerance is refused, so a captured event cannot be replayed later. An event about a PaymentIntent carries it as
/// the unified <see cref="Charge"/>, mapped by <paramref name="mappers"/>' charge mapper.
/// </summary>
public sealed class StripeNotificationOperations(
    ProviderInfo info,
    string? webhookSecret,
    TimeSpan tolerance,
    TimeProvider clock,
    IMapperFactory? mappers) : INotificationOperations
{
    public const string SignatureHeader = "Stripe-Signature";

    /// <summary>Operations whose notifications carry no charge, as before they could map one.</summary>
    public StripeNotificationOperations(ProviderInfo info, string? webhookSecret, TimeSpan tolerance, TimeProvider clock)
        : this(info, webhookSecret, tolerance, clock, mappers: null)
    {
    }

    // The last second DateTimeOffset can represent: a larger timestamp is not one.
    private const long MaxUnixSeconds = 253_402_300_799;

    public PaymentResponse<PaymentNotification> Verify(NotificationRequest request)
    {
        if (string.IsNullOrEmpty(webhookSecret))
        {
            return Fail("webhook_secret_missing", "StripeOptions.WebhookSecret is not set, so no notification can be verified.");
        }

        var header = request.Headers
            .FirstOrDefault(pair => string.Equals(pair.Key, SignatureHeader, StringComparison.OrdinalIgnoreCase))
            .Value;
        if (string.IsNullOrWhiteSpace(header))
        {
            return Fail("signature_missing", "The notification carries no Stripe-Signature header.");
        }

        if (!TryParseHeader(header, out var timestamp, out var signatures))
        {
            return Fail("signature_malformed", "The Stripe-Signature header has no valid timestamp or no v1 signature.");
        }

        var expected = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(webhookSecret),
            Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{timestamp}.{request.Body}")));
        if (!signatures.Any(signature => CryptographicOperations.FixedTimeEquals(signature, expected)))
        {
            return Fail("signature_invalid", "No v1 signature in the Stripe-Signature header matches the notification.");
        }

        var sentAt = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        if ((clock.GetUtcNow() - sentAt).Duration() > tolerance)
        {
            return Fail("timestamp_outside_tolerance", $"The notification was signed at {sentAt:O}, further from now than {tolerance} allows.");
        }

        return Parse(request.Body);
    }

    private static bool TryParseHeader(string header, out long timestamp, out List<byte[]> signatures)
    {
        long? parsedTimestamp = null;
        signatures = [];

        foreach (var part in header.Split(','))
        {
            var pair = part.Split('=', 2);
            if (pair.Length != 2)
            {
                continue;
            }

            var value = pair[1].Trim();
            switch (pair[0].Trim())
            {
                case "t" when long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) && seconds <= MaxUnixSeconds:
                    parsedTimestamp = seconds;
                    break;
                case "v1" when TryParseHex(value, out var signature):
                    signatures.Add(signature);
                    break;
            }
        }

        timestamp = parsedTimestamp ?? 0;
        return parsedTimestamp is not null && signatures.Count > 0;
    }

    private static bool TryParseHex(string value, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromHexString(value);
            return bytes.Length == 32;
        }
        catch (FormatException)
        {
            bytes = [];
            return false;
        }
    }

    private PaymentResponse<PaymentNotification> Parse(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            // Any JSON can carry a valid signature once the secret signs it; only an event is one.
            if (root.GetProperty("object").GetString() != "event")
            {
                throw new JsonException("Its object is not 'event'.");
            }

            string? objectId = null;
            string? objectType = null;
            Charge? charge = null;
            if (root.TryGetProperty("data", out var data)
                && data.TryGetProperty("object", out var @object)
                && @object.ValueKind == JsonValueKind.Object)
            {
                objectId = @object.TryGetProperty("id", out var id) ? id.GetString() : null;
                objectType = @object.TryGetProperty("object", out var type) ? type.GetString() : null;
                charge = objectType == "payment_intent" ? ChargeOf(@object) : null;
            }

            var notification = new PaymentNotification
            {
                Id = root.GetProperty("id").GetString() ?? throw new JsonException("The event has no id."),
                Type = root.GetProperty("type").GetString() ?? throw new JsonException("The event has no type."),
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(root.GetProperty("created").GetInt64()),
                ObjectId = objectId,
                ObjectType = objectType,
                LiveMode = root.GetProperty("livemode").GetBoolean(),
                ApiVersion = root.TryGetProperty("api_version", out var apiVersion) && apiVersion.ValueKind == JsonValueKind.String
                    ? apiVersion.GetString()
                    : null,
                Charge = charge,
            };

            // The event's PaymentIntent holds its client secret, which an audit trail of notifications must not keep.
            return PaymentResponse<PaymentNotification>.Ok(info, notification, StripeRawBody.WithoutClientSecrets(body));
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException or ArgumentOutOfRangeException)
        {
            return Fail("payload_invalid", $"The notification is signed but is not a Stripe event: {exception.Message}");
        }
    }

    /// <summary>
    /// The event's PaymentIntent as a charge, or null when it cannot be read as one: the event is still Stripe's, and
    /// the consumer still has to acknowledge it, so a PaymentIntent the model does not fit leaves the charge out
    /// rather than failing the notification.
    /// </summary>
    private Charge? ChargeOf(JsonElement paymentIntent)
    {
        if (mappers is null)
        {
            return null;
        }

        try
        {
            var intent = paymentIntent.Deserialize<StripePaymentIntent>(StripeJson.Options);
            return intent is null ? null : mappers.GetMapper<StripePaymentIntent, Charge>().Map(intent);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private PaymentResponse<PaymentNotification> Fail(string code, string message) =>
        PaymentResponse<PaymentNotification>.Fail(info, new PaymentError { Code = code, Message = message });
}
