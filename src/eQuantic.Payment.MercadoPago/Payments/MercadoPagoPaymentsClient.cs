using eQuantic.Payment.Http;
using eQuantic.Payment.MercadoPago.Payments.Models;

namespace eQuantic.Payment.MercadoPago.Payments;

/// <summary>
/// Typed client for the Mercado Pago Payments API (<c>/v1/payments</c>).
/// JSON wire format; Bearer auth; <c>X-Idempotency-Key</c> on create/refund, the caller's or a fresh one, and on
/// capture/cancel when the caller gives one.
/// </summary>
public class MercadoPagoPaymentsClient(HttpClient httpClient) : MercadoPagoClientBase(httpClient)
{
    public Task<ApiResult<MercadoPagoPaymentResponse>> CreatePaymentAsync(MercadoPagoPaymentRequest request, CancellationToken cancellationToken = default)
        => CreatePaymentAsync(request, idempotencyKey: null, cancellationToken);

    public Task<ApiResult<MercadoPagoPaymentResponse>> CreatePaymentAsync(MercadoPagoPaymentRequest request, string? idempotencyKey, CancellationToken cancellationToken)
        => SendIdempotentAsync<MercadoPagoPaymentResponse>(HttpMethod.Post, "v1/payments", request, idempotencyKey, cancellationToken);

    public Task<ApiResult<MercadoPagoPaymentResponse>> GetPaymentAsync(long paymentId, CancellationToken cancellationToken = default)
        => SendJsonAsync<MercadoPagoPaymentResponse>(HttpMethod.Get, $"v1/payments/{paymentId}", null, cancellationToken);

    public Task<ApiResult<MercadoPagoPaymentResponse>> CapturePaymentAsync(long paymentId, MercadoPagoCaptureRequest request, CancellationToken cancellationToken = default)
        => SendJsonAsync<MercadoPagoPaymentResponse>(HttpMethod.Put, $"v1/payments/{paymentId}", request, cancellationToken);

    public Task<ApiResult<MercadoPagoPaymentResponse>> CapturePaymentAsync(long paymentId, MercadoPagoCaptureRequest request, string? idempotencyKey, CancellationToken cancellationToken)
        => idempotencyKey is null
            ? CapturePaymentAsync(paymentId, request, cancellationToken)
            : SendIdempotentAsync<MercadoPagoPaymentResponse>(HttpMethod.Put, $"v1/payments/{paymentId}", request, idempotencyKey, cancellationToken);

    public Task<ApiResult<MercadoPagoPaymentResponse>> CancelPaymentAsync(long paymentId, CancellationToken cancellationToken = default)
        => SendJsonAsync<MercadoPagoPaymentResponse>(HttpMethod.Put, $"v1/payments/{paymentId}", new MercadoPagoCancelRequest(), cancellationToken);

    public Task<ApiResult<MercadoPagoPaymentResponse>> CancelPaymentAsync(long paymentId, string? idempotencyKey, CancellationToken cancellationToken)
        => idempotencyKey is null
            ? CancelPaymentAsync(paymentId, cancellationToken)
            : SendIdempotentAsync<MercadoPagoPaymentResponse>(HttpMethod.Put, $"v1/payments/{paymentId}", new MercadoPagoCancelRequest(), idempotencyKey, cancellationToken);

    public Task<ApiResult<MercadoPagoRefundResponse>> CreateRefundAsync(long paymentId, MercadoPagoRefundRequest request, CancellationToken cancellationToken = default)
        => CreateRefundAsync(paymentId, request, idempotencyKey: null, cancellationToken);

    public Task<ApiResult<MercadoPagoRefundResponse>> CreateRefundAsync(long paymentId, MercadoPagoRefundRequest request, string? idempotencyKey, CancellationToken cancellationToken)
        => SendIdempotentAsync<MercadoPagoRefundResponse>(HttpMethod.Post, $"v1/payments/{paymentId}/refunds", request.Amount is null ? null : request, idempotencyKey, cancellationToken);
}
