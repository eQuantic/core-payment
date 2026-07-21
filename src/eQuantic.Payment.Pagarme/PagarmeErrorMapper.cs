using eQuantic.Payment.Http;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Pagarme;

/// <summary>
/// Builds a normalized <see cref="PaymentError"/> from a failed Pagar.me HTTP result.
/// Error parsing is not object-to-object mapping, so it stays a small helper (not an <c>IMapper</c>).
/// </summary>
internal static class PagarmeErrorMapper
{
    public static PaymentError ToError<T>(ApiResult<T> result, string version) => new()
    {
        Message = string.IsNullOrWhiteSpace(result.RawBody)
            ? $"Pagar.me {version} request failed (HTTP {result.StatusCode})."
            : result.RawBody!,
        HttpStatusCode = result.StatusCode,
    };
}
