using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using eQuantic.Payment.Http;

namespace eQuantic.Payment.MercadoPago;

/// <summary>
/// Base for the Mercado Pago typed clients. Shares the JSON wire settings and adds the mandatory
/// <c>X-Idempotency-Key</c> header on state-changing POSTs (payments, orders, refunds).
/// </summary>
public abstract class MercadoPagoClientBase(HttpClient httpClient) : PaymentHttpClientBase(httpClient)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    protected override JsonSerializerOptions JsonOptions => SerializerOptions;

    /// <summary>Sends a JSON POST/PUT carrying a fresh idempotency key.</summary>
    protected Task<ApiResult<TResponse>> SendIdempotentAsync<TResponse>(
        HttpMethod method, string path, object? body, CancellationToken cancellationToken)
        => SendIdempotentAsync<TResponse>(method, path, body, idempotencyKey: null, cancellationToken);

    /// <summary>Sends a JSON POST/PUT carrying the caller's idempotency key, or a fresh one when it is null.</summary>
    protected Task<ApiResult<TResponse>> SendIdempotentAsync<TResponse>(
        HttpMethod method, string path, object? body, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation("X-Idempotency-Key", idempotencyKey ?? Guid.NewGuid().ToString());
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        return SendAsync<TResponse>(request, cancellationToken);
    }
}
