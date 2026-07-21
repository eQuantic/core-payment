using eQuantic.Payment.Http;
using eQuantic.Payment.MercadoPago.Payments.Models;

namespace eQuantic.Payment.MercadoPago.Payments;

/// <summary>
/// Typed client for the Mercado Pago Payments API (<c>/v1/payments</c>).
/// JSON wire format; Bearer auth; <c>X-Idempotency-Key</c> on create/refund.
/// </summary>
public class MercadoPagoPaymentsClient(HttpClient httpClient) : MercadoPagoClientBase(httpClient)
{
    public Task<ApiResult<MercadoPagoPaymentResponse>> CreatePaymentAsync(MercadoPagoPaymentRequest request, CancellationToken cancellationToken = default)
        => SendIdempotentAsync<MercadoPagoPaymentResponse>(HttpMethod.Post, "v1/payments", request, cancellationToken);

    public Task<ApiResult<MercadoPagoPaymentResponse>> GetPaymentAsync(long paymentId, CancellationToken cancellationToken = default)
        => SendJsonAsync<MercadoPagoPaymentResponse>(HttpMethod.Get, $"v1/payments/{paymentId}", null, cancellationToken);

    public Task<ApiResult<MercadoPagoPaymentResponse>> CapturePaymentAsync(long paymentId, MercadoPagoCaptureRequest request, CancellationToken cancellationToken = default)
        => SendJsonAsync<MercadoPagoPaymentResponse>(HttpMethod.Put, $"v1/payments/{paymentId}", request, cancellationToken);

    public Task<ApiResult<MercadoPagoPaymentResponse>> CancelPaymentAsync(long paymentId, CancellationToken cancellationToken = default)
        => SendJsonAsync<MercadoPagoPaymentResponse>(HttpMethod.Put, $"v1/payments/{paymentId}", new MercadoPagoCancelRequest(), cancellationToken);

    public Task<ApiResult<MercadoPagoRefundResponse>> CreateRefundAsync(long paymentId, MercadoPagoRefundRequest request, CancellationToken cancellationToken = default)
        => SendIdempotentAsync<MercadoPagoRefundResponse>(HttpMethod.Post, $"v1/payments/{paymentId}/refunds", request.Amount is null ? null : request, cancellationToken);
}
