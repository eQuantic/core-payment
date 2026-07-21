using System.Text.Json;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Cielo;

/// <summary>
/// Normalizes a failed Cielo HTTP result into a <see cref="PaymentError"/>. On validation/business errors Cielo
/// returns a JSON array of <c>{ "Code": int, "Message": string }</c> objects (always an array, even for one error),
/// parsed defensively with <see cref="JsonDocument"/>. Not object-to-object mapping, so it stays a helper.
/// </summary>
internal static class CieloErrorMapper
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

                var first = root.ValueKind switch
                {
                    JsonValueKind.Array when root.GetArrayLength() > 0 => root[0],
                    JsonValueKind.Object => root,
                    _ => default,
                };

                if (first.ValueKind == JsonValueKind.Object)
                {
                    if (first.TryGetProperty("Code", out var c))
                    {
                        code = c.ValueKind == JsonValueKind.Number ? c.GetRawText() : c.GetString();
                    }

                    if (first.TryGetProperty("Message", out var m) && m.ValueKind == JsonValueKind.String)
                    {
                        message = m.GetString();
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
            Message = message ?? $"Cielo request failed (HTTP {result.StatusCode}).",
            HttpStatusCode = result.StatusCode,
        };
    }
}
