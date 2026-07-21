using System.Text.Json;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Efi;

/// <summary>
/// Normalizes a failed Efí HTTP result into a <see cref="PaymentError"/>. Handles both wire shapes defensively:
/// the Pix API's RFC 7807 <c>{ type, title, status, detail, violacoes }</c> and the Cobranças API's
/// <c>{ code, error, error_description }</c> (where <c>error_description</c> may be a string or an object).
/// </summary>
internal static class EfiErrorMapper
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

                // Pix (RFC 7807)
                if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                {
                    message = title.GetString();
                    if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                    {
                        message = $"{message} {detail.GetString()}".Trim();
                    }
                }

                // Cobranças
                if (root.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                {
                    code = error.GetString();
                }

                if (root.TryGetProperty("code", out var c))
                {
                    code = c.ValueKind == JsonValueKind.Number ? c.GetRawText() : c.GetString() ?? code;
                }

                if (message is null && root.TryGetProperty("error_description", out var desc))
                {
                    message = desc.ValueKind switch
                    {
                        JsonValueKind.String => desc.GetString(),
                        JsonValueKind.Object => desc.TryGetProperty("message", out var m) ? m.GetString() : desc.GetRawText(),
                        _ => null,
                    };
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
            Message = message ?? $"Efí request failed (HTTP {result.StatusCode}).",
            HttpStatusCode = result.StatusCode,
        };
    }
}
