using System.Text.Json;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Asaas;

/// <summary>
/// Normalizes a failed Asaas HTTP result (<c>{ "errors": [ { "code", "description" } ] }</c>) into a
/// <see cref="PaymentError"/>. Parsed defensively with <see cref="JsonDocument"/>; not object-to-object mapping,
/// so it stays a plain helper rather than an <c>IMapper</c>.
/// </summary>
internal static class AsaasErrorMapper
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
                if (doc.RootElement.TryGetProperty("errors", out var errors) &&
                    errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
                {
                    var first = errors[0];
                    if (first.TryGetProperty("code", out var c))
                    {
                        code = c.ValueKind == JsonValueKind.Number ? c.GetRawText() : c.GetString();
                    }

                    if (first.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String)
                    {
                        message = d.GetString();
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
            Message = message ?? $"Asaas request failed (HTTP {result.StatusCode}).",
            HttpStatusCode = result.StatusCode,
        };
    }
}
