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
    public static PaymentError ToError<T>(ApiResult<T> result) => ToError(Parse(result), result.StatusCode);

    public static PaymentError ToError(StripeErrorDetail? error, int? statusCode) => new()
    {
        Code = error?.DeclineCode ?? error?.Code ?? error?.Type,
        Message = error?.Message ?? $"Stripe request failed (HTTP {statusCode}).",
        HttpStatusCode = statusCode,
    };

    /// <summary>The error a failed call answered with, or null when its body is not Stripe's error envelope.</summary>
    public static StripeErrorDetail? Parse<T>(ApiResult<T> result)
    {
        if (string.IsNullOrWhiteSpace(result.RawBody))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<StripeErrorEnvelope>(result.RawBody, StripeJson.Options)?.Error;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

internal static class StripeJson
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
}
