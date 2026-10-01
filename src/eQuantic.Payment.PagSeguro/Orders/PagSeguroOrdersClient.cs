using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using eQuantic.Payment.Http;
using eQuantic.Payment.PagSeguro.Orders.Models;

namespace eQuantic.Payment.PagSeguro.Orders;

/// <summary>
/// Typed client for the PagSeguro Orders/Charges API (<c>/orders</c>, <c>/charges</c>).
/// JSON wire format; Bearer auth; <c>x-idempotency-key</c> on create, capture and cancel, the caller's or a fresh one.
/// </summary>
public class PagSeguroOrdersClient(HttpClient httpClient) : PaymentHttpClientBase(httpClient)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    protected override JsonSerializerOptions JsonOptions => SerializerOptions;

    public Task<ApiResult<PagSeguroOrderResponse>> CreateOrderAsync(PagSeguroOrderRequest request, CancellationToken cancellationToken = default)
        => CreateOrderAsync(request, idempotencyKey: null, cancellationToken);

    public Task<ApiResult<PagSeguroOrderResponse>> CreateOrderAsync(PagSeguroOrderRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
        => SendIdempotentJsonAsync<PagSeguroOrderResponse>(HttpMethod.Post, "orders", request, idempotencyKey, cancellationToken);

    public Task<ApiResult<PagSeguroOrderResponse>> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
        => SendJsonAsync<PagSeguroOrderResponse>(HttpMethod.Get, $"orders/{orderId}", null, cancellationToken);

    public Task<ApiResult<PagSeguroChargeResponse>> GetChargeAsync(string chargeId, CancellationToken cancellationToken = default)
        => SendJsonAsync<PagSeguroChargeResponse>(HttpMethod.Get, $"charges/{chargeId}", null, cancellationToken);

    public Task<ApiResult<PagSeguroChargeResponse>> CaptureChargeAsync(string chargeId, long? amount, CancellationToken cancellationToken = default)
        => CaptureChargeAsync(chargeId, amount, idempotencyKey: null, cancellationToken);

    public Task<ApiResult<PagSeguroChargeResponse>> CaptureChargeAsync(string chargeId, long? amount, string? idempotencyKey, CancellationToken cancellationToken = default)
        => SendIdempotentJsonAsync<PagSeguroChargeResponse>(HttpMethod.Post, $"charges/{chargeId}/capture", AmountBody(amount), idempotencyKey, cancellationToken);

    /// <summary>Cancels a pre-auth or refunds a captured charge (same endpoint; pass <paramref name="amount"/> for partial).</summary>
    public Task<ApiResult<PagSeguroChargeResponse>> CancelChargeAsync(string chargeId, long? amount, CancellationToken cancellationToken = default)
        => CancelChargeAsync(chargeId, amount, idempotencyKey: null, cancellationToken);

    /// <inheritdoc cref="CancelChargeAsync(string, long?, CancellationToken)"/>
    public Task<ApiResult<PagSeguroChargeResponse>> CancelChargeAsync(string chargeId, long? amount, string? idempotencyKey, CancellationToken cancellationToken = default)
        => SendIdempotentJsonAsync<PagSeguroChargeResponse>(HttpMethod.Post, $"charges/{chargeId}/cancel", AmountBody(amount), idempotencyKey, cancellationToken);

    private static PagSeguroAmountEnvelope? AmountBody(long? amount) =>
        amount is { } v ? new PagSeguroAmountEnvelope { Amount = new PagSeguroAmountRequest { Value = v } } : null;

    private Task<ApiResult<TResponse>> SendIdempotentJsonAsync<TResponse>(HttpMethod method, string path, object? body, string? idempotencyKey, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation("x-idempotency-key", idempotencyKey ?? Guid.NewGuid().ToString());
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        return SendAsync<TResponse>(request, cancellationToken);
    }
}
