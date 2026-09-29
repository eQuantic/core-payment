using System.Security.Cryptography;
using System.Text;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Stripe;
using eQuantic.Payment.Stripe.V1;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

public class StripeNotificationTests
{
    private const string Secret = "whsec_test_secret";
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_770_000_000);

    private const string EventJson = """
    {
      "id": "evt_1",
      "object": "event",
      "api_version": "2025-04-30.basil",
      "created": 1769999990,
      "livemode": false,
      "type": "payment_intent.succeeded",
      "data": { "object": { "id": "pi_123", "object": "payment_intent", "amount": 2500 } }
    }
    """;

    private static string Sign(string body, long timestamp, string secret = Secret) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{body}")));

    private static INotificationOperations Notifications(string? secret = Secret) =>
        new StripeProviderV1(
            new StripeClientV1(new HttpClient()), StripeApiVersion.V2025_04_30_Basil, TestMapperFactory.Create(),
            secret, StripeDefaults.WebhookTolerance, new FixedClock(Now)).Notifications;

    // Header names arrive in whatever case the host hands them over.
    private static NotificationRequest Request(string body, string? signature) => new()
    {
        Body = body,
        Headers = signature is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["stripe-signature"] = signature },
    };

    private static string Header(long timestamp, params string[] signatures) =>
        string.Join(',', new[] { $"t={timestamp}" }.Concat(signatures.Select(signature => $"v1={signature}")));

    [Fact]
    public void A_genuine_event_is_verified_and_parsed()
    {
        var t = Now.ToUnixTimeSeconds();

        var response = Notifications().Verify(Request(EventJson, Header(t, Sign(EventJson, t))));

        Assert.True(response.Success, response.Error?.Message);
        Assert.Equal("stripe@2025-04-30.basil", response.Provider.Key);
        var notification = response.Data!;
        Assert.Equal("evt_1", notification.Id);
        Assert.Equal("payment_intent.succeeded", notification.Type);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1_769_999_990), notification.CreatedAt);
        Assert.Equal("pi_123", notification.ObjectId);
        Assert.Equal("payment_intent", notification.ObjectType);
        Assert.False(notification.LiveMode);
        Assert.Equal("2025-04-30.basil", notification.ApiVersion);
        Assert.Equal(EventJson, response.RawResponse);
    }

    [Fact]
    public void Any_one_signature_is_enough_while_a_secret_is_rolled()
    {
        var t = Now.ToUnixTimeSeconds();

        var response = Notifications().Verify(Request(EventJson, Header(t, Sign(EventJson, t, "whsec_old_secret"), Sign(EventJson, t))));

        Assert.True(response.Success, response.Error?.Message);
    }

    [Fact]
    public void A_forged_or_altered_event_is_refused()
    {
        var t = Now.ToUnixTimeSeconds();
        var altered = EventJson.Replace("2500", "1");

        Assert.Equal("signature_invalid", Notifications().Verify(Request(EventJson, Header(t, Sign(EventJson, t, "whsec_forger")))).Error!.Code);
        Assert.Equal("signature_invalid", Notifications().Verify(Request(altered, Header(t, Sign(EventJson, t)))).Error!.Code);
    }

    [Theory]
    [InlineData(-301)]
    [InlineData(301)]
    public void An_event_signed_too_far_from_now_is_refused(int secondsFromNow)
    {
        var t = Now.ToUnixTimeSeconds() + secondsFromNow;

        var response = Notifications().Verify(Request(EventJson, Header(t, Sign(EventJson, t))));

        Assert.False(response.Success);
        Assert.Equal("timestamp_outside_tolerance", response.Error!.Code);
    }

    [Fact]
    public void Without_a_header_a_timestamp_or_a_secret_nothing_is_verified()
    {
        var t = Now.ToUnixTimeSeconds();
        var signed = Header(t, Sign(EventJson, t));

        Assert.Equal("signature_missing", Notifications().Verify(Request(EventJson, null)).Error!.Code);
        Assert.Equal("signature_malformed", Notifications().Verify(Request(EventJson, $"v1={Sign(EventJson, t)}")).Error!.Code);
        Assert.Equal("signature_malformed", Notifications().Verify(Request(EventJson, $"t={t},v1=not-hex")).Error!.Code);
        Assert.Equal("webhook_secret_missing", Notifications(secret: null).Verify(Request(EventJson, signed)).Error!.Code);
    }

    [Fact]
    public void A_provider_built_as_in_1_0_0_has_no_secret_and_refuses_every_notification()
    {
        var t = Now.ToUnixTimeSeconds();
        var provider = new StripeProviderV1(
            new StripeClientV1(new HttpClient()), StripeApiVersion.V2025_04_30_Basil, TestMapperFactory.Create());

        var response = provider.Notifications.Verify(Request(EventJson, Header(t, Sign(EventJson, t))));

        Assert.False(response.Success);
        Assert.Equal("webhook_secret_missing", response.Error!.Code);
    }

    [Fact]
    public void A_signed_body_that_is_not_an_event_is_refused()
    {
        var t = Now.ToUnixTimeSeconds();
        const string body = """{ "hello": "world" }""";

        var response = Notifications().Verify(Request(body, Header(t, Sign(body, t))));

        Assert.Equal("payload_invalid", response.Error!.Code);
    }

    [Fact]
    public void A_provider_that_does_not_verify_its_notifications_says_so()
    {
        IPaymentProvider provider = new NotifyingNothingProvider();

        var response = provider.Notifications.Verify(Request(EventJson, null));

        Assert.False(response.Success);
        Assert.Equal("notifications_unsupported", response.Error!.Code);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class NotifyingNothingProvider : IPaymentProvider
    {
        public ProviderInfo Info { get; } = new("quiet", "v1");
        public IChargeOperations Charges => throw new NotSupportedException();
        public IRefundOperations Refunds => throw new NotSupportedException();
        public ICustomerOperations Customers => throw new NotSupportedException();
    }
}
