using eQuantic.Payment.Http;
using eQuantic.Payment.MercadoPago.Orders.Models;

namespace eQuantic.Payment.MercadoPago.Orders;

/// <summary>
/// Typed client for the Mercado Pago Orders API (<c>/v1/orders</c>).
/// JSON wire format; Bearer auth; <c>X-Idempotency-Key</c> on create/capture/cancel/refund, the caller's or a fresh one.
/// </summary>
public class MercadoPagoOrdersClient(HttpClient httpClient) : MercadoPagoClientBase(httpClient)
{
    public Task<ApiResult<MercadoPagoOrderResponse>> CreateOrderAsync(MercadoPagoOrderRequest request, CancellationToken cancellationToken = default)
        => CreateOrderAsync(request, idempotencyKey: null, cancellationToken);

    public Task<ApiResult<MercadoPagoOrderResponse>> CreateOrderAsync(MercadoPagoOrderRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
        => SendIdempotentAsync<MercadoPagoOrderResponse>(HttpMethod.Post, "v1/orders", request, idempotencyKey, cancellationToken);

    public Task<ApiResult<MercadoPagoOrderResponse>> GetOrderAsync(string orderId, CancellationToken cancellationToken = default)
        => SendJsonAsync<MercadoPagoOrderResponse>(HttpMethod.Get, $"v1/orders/{orderId}", null, cancellationToken);

    public Task<ApiResult<MercadoPagoOrderResponse>> CaptureOrderAsync(string orderId, CancellationToken cancellationToken = default)
        => CaptureOrderAsync(orderId, idempotencyKey: null, cancellationToken);

    public Task<ApiResult<MercadoPagoOrderResponse>> CaptureOrderAsync(string orderId, string? idempotencyKey, CancellationToken cancellationToken = default)
        => SendIdempotentAsync<MercadoPagoOrderResponse>(HttpMethod.Post, $"v1/orders/{orderId}/capture", null, idempotencyKey, cancellationToken);

    public Task<ApiResult<MercadoPagoOrderResponse>> CancelOrderAsync(string orderId, CancellationToken cancellationToken = default)
        => CancelOrderAsync(orderId, idempotencyKey: null, cancellationToken);

    public Task<ApiResult<MercadoPagoOrderResponse>> CancelOrderAsync(string orderId, string? idempotencyKey, CancellationToken cancellationToken = default)
        => SendIdempotentAsync<MercadoPagoOrderResponse>(HttpMethod.Post, $"v1/orders/{orderId}/cancel", null, idempotencyKey, cancellationToken);

    public Task<ApiResult<MercadoPagoOrderResponse>> RefundOrderAsync(string orderId, MercadoPagoOrderRefundRequest request, CancellationToken cancellationToken = default)
        => RefundOrderAsync(orderId, request, idempotencyKey: null, cancellationToken);

    public Task<ApiResult<MercadoPagoOrderResponse>> RefundOrderAsync(string orderId, MercadoPagoOrderRefundRequest request, string? idempotencyKey, CancellationToken cancellationToken = default)
        => SendIdempotentAsync<MercadoPagoOrderResponse>(HttpMethod.Post, $"v1/orders/{orderId}/refund", request.Amount is null ? null : request, idempotencyKey, cancellationToken);
}
