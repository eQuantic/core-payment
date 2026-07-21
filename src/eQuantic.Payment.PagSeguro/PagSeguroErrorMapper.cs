using System.Text.Json;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.PagSeguro;

/// <summary>
/// Normalizes a failed PagSeguro HTTP result (<c>{ "error_messages": [ { code, description, parameter_name } ] }</c>)
/// into a <see cref="PaymentError"/>. Not object-to-object mapping, so it stays a helper.
/// </summary>
internal static class PagSeguroErrorMapper
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
                if (doc.RootElement.TryGetProperty("error_messages", out var errors) &&
                    errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
                {
                    var first = errors[0];
                    if (first.TryGetProperty("code", out var c))
                    {
                        code = c.ValueKind == JsonValueKind.Number ? c.GetRawText() : c.GetString();
                    }

                    var description = first.TryGetProperty("description", out var d) ? d.GetString() : null;
                    var parameter = first.TryGetProperty("parameter_name", out var p) ? p.GetString() : null;
                    message = string.IsNullOrWhiteSpace(parameter) ? description : $"{description} ({parameter})";
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
            Message = message ?? $"PagSeguro request failed (HTTP {result.StatusCode}).",
            HttpStatusCode = result.StatusCode,
        };
    }
}
