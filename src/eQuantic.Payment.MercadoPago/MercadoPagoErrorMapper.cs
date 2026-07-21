using System.Text.Json;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.MercadoPago;

/// <summary>
/// Normalizes a failed Mercado Pago HTTP result into a <see cref="PaymentError"/>. Parsed defensively with
/// <see cref="JsonDocument"/> because MP's error shape varies (<c>cause</c> may be an array or a single object,
/// and <c>cause[].code</c> may be a string or a number). Not object-to-object mapping, so it stays a helper.
/// </summary>
internal static class MercadoPagoErrorMapper
{
    public static PaymentError ToError<T>(ApiResult<T> result)
    {
        string? message = null;
        string? code = null;

        if (!string.IsNullOrWhiteSpace(result.RawBody))
        {
            try
            {
                using var doc = JsonDocument.Parse(result.RawBody!);
                var root = doc.RootElement;

                if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                {
                    message = msg.GetString();
                }

                if (root.TryGetProperty("error", out var err) && err.ValueKind == JsonValueKind.String)
                {
                    code = err.GetString();
                }

                if (root.TryGetProperty("cause", out var cause))
                {
                    var first = cause.ValueKind switch
                    {
                        JsonValueKind.Array when cause.GetArrayLength() > 0 => cause[0],
                        JsonValueKind.Object => cause,
                        _ => default,
                    };

                    if (first.ValueKind == JsonValueKind.Object)
                    {
                        if (first.TryGetProperty("code", out var causeCode))
                        {
                            code = causeCode.ValueKind == JsonValueKind.Number
                                ? causeCode.GetRawText()
                                : causeCode.GetString() ?? code;
                        }

                        if (first.TryGetProperty("description", out var desc) && desc.ValueKind == JsonValueKind.String)
                        {
                            message = desc.GetString() ?? message;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Fall through to the generic message below.
            }
        }

        return new PaymentError
        {
            Code = code,
            Message = message ?? $"Mercado Pago request failed (HTTP {result.StatusCode}).",
            HttpStatusCode = result.StatusCode,
        };
    }
}
