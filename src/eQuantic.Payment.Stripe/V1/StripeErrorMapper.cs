using System.Text.Json;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1;

/// <summary>
/// Normalizes a failed Stripe HTTP result into a <see cref="PaymentError"/>.
/// Error parsing is not object-to-object mapping, so it stays a small helper (not an <c>IMapper</c>).
/// </summary>
internal static class StripeErrorMapper
{
    public static PaymentError ToError<T>(ApiResult<T> result)
    {
        StripeErrorDetail? error = null;
        if (!string.IsNullOrWhiteSpace(result.RawBody))
        {
            try
            {
                error = JsonSerializer.Deserialize<StripeErrorEnvelope>(result.RawBody, StripeJson.Options)?.Error;
            }
            catch (JsonException)
            {
                // Fall through to a generic error below.
            }
        }

        return new PaymentError
        {
            Code = error?.DeclineCode ?? error?.Code ?? error?.Type,
            Message = error?.Message ?? $"Stripe request failed (HTTP {result.StatusCode}).",
            HttpStatusCode = result.StatusCode,
        };
    }
}

internal static class StripeJson
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
}
