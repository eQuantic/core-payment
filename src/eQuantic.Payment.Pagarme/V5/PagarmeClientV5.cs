using System.Text.Json;
using System.Text.Json.Serialization;
using eQuantic.Payment.Http;
using eQuantic.Payment.Pagarme.V5.Models;

namespace eQuantic.Payment.Pagarme.V5;

/// <summary>
/// Typed client for the Pagar.me Core API v5 (<c>https://api.pagar.me/core/v5</c>).
/// Order/charge-based: payments are created as orders and managed as charges.
/// </summary>
public class PagarmeClientV5(HttpClient httpClient) : PaymentHttpClientBase(httpClient)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    protected override JsonSerializerOptions JsonOptions => SerializerOptions;

    public Task<ApiResult<V5OrderResponse>> CreateOrderAsync(V5OrderRequest request, CancellationToken cancellationToken = default)
        => SendJsonAsync<V5OrderResponse>(HttpMethod.Post, "orders", request, cancellationToken);

    public Task<ApiResult<V5OrderResponse>> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
        => SendJsonAsync<V5OrderResponse>(HttpMethod.Get, $"orders/{orderId}", null, cancellationToken);

    public Task<ApiResult<V5ChargeResponse>> GetChargeAsync(string chargeId, CancellationToken cancellationToken = default)
        => SendJsonAsync<V5ChargeResponse>(HttpMethod.Get, $"charges/{chargeId}", null, cancellationToken);

    public Task<ApiResult<V5ChargeResponse>> CaptureChargeAsync(string chargeId, long? amount = null, CancellationToken cancellationToken = default)
        => SendJsonAsync<V5ChargeResponse>(HttpMethod.Post, $"charges/{chargeId}/capture", new V5CaptureRequest { Amount = amount }, cancellationToken);

    /// <summary>Cancels a pending charge or refunds a paid one (v5 uses DELETE for both; pass <paramref name="amount"/> for partial refund).</summary>
    public Task<ApiResult<V5ChargeResponse>> CancelChargeAsync(string chargeId, long? amount = null, CancellationToken cancellationToken = default)
        => SendJsonAsync<V5ChargeResponse>(HttpMethod.Delete, $"charges/{chargeId}", amount is null ? null : new V5CancelRequest { Amount = amount }, cancellationToken);

    public Task<ApiResult<V5CustomerResponse>> CreateCustomerAsync(V5CustomerRequest request, CancellationToken cancellationToken = default)
        => SendJsonAsync<V5CustomerResponse>(HttpMethod.Post, "customers", request, cancellationToken);

    public Task<ApiResult<V5CustomerResponse>> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        => SendJsonAsync<V5CustomerResponse>(HttpMethod.Get, $"customers/{customerId}", null, cancellationToken);
}
