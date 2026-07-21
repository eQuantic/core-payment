using System.Text.Json;
using System.Text.Json.Serialization;
using eQuantic.Payment.Adyen.V71.Models;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Adyen;

/// <summary>
/// Normalizes a failed Adyen HTTP result (a <see cref="AdyenServiceError"/> with
/// <c>{ status, errorCode, message, errorType, pspReference }</c>) into a <see cref="PaymentError"/>.
/// Error parsing is not object-to-object mapping, so it stays a small helper (not an <c>IMapper</c>).
/// </summary>
internal static class AdyenErrorMapper
{
    public static PaymentError ToError<T>(ApiResult<T> result)
    {
        AdyenServiceError? error = null;
        if (!string.IsNullOrWhiteSpace(result.RawBody))
        {
            try
            {
                error = JsonSerializer.Deserialize<AdyenServiceError>(result.RawBody, AdyenJson.Options);
            }
            catch (JsonException)
            {
                // Fall through to a generic error below.
            }
        }

        return new PaymentError
        {
            Code = error?.ErrorCode,
            Message = error?.Message ?? $"Adyen request failed (HTTP {result.StatusCode}).",
            HttpStatusCode = result.StatusCode,
        };
    }
}

/// <summary>Shared serializer options for the Adyen wire format (camelCase JSON; nulls omitted on write).</summary>
internal static class AdyenJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };
}
