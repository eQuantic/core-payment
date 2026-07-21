using System.Text;
using System.Text.Json;
using eQuantic.Payment.Adyen.V71.Models;
using eQuantic.Payment.Http;

namespace eQuantic.Payment.Adyen.V71;

/// <summary>
/// Typed client for the Adyen Checkout API v71 (<c>/v71/payments</c> and the payment-modification endpoints).
/// JSON wire format (camelCase); auth is the <c>X-API-Key</c> header (configured on the named HttpClient);
/// a fresh <c>Idempotency-Key</c> is sent on every POST.
/// </summary>
public class AdyenClientV71(HttpClient httpClient) : PaymentHttpClientBase(httpClient)
{
    private static readonly string VersionSegment = AdyenDefaults.VersionPath(AdyenApiVersion.V71);

    protected override JsonSerializerOptions JsonOptions => AdyenJson.Options;

    /// <summary>Creates a payment (<c>POST /v71/payments</c>).</summary>
    public Task<ApiResult<AdyenPaymentResponse>> CreatePaymentAsync(AdyenPaymentRequest request, CancellationToken cancellationToken = default)
        => SendIdempotentJsonAsync<AdyenPaymentResponse>(HttpMethod.Post, $"{VersionSegment}/payments", request, cancellationToken);

    /// <summary>Captures a payment (<c>POST /v71/payments/{pspReference}/captures</c>). Asynchronous; returns a "received" ack.</summary>
    public Task<ApiResult<AdyenModificationResponse>> CaptureAsync(string pspReference, AdyenCaptureRequest request, CancellationToken cancellationToken = default)
        => SendIdempotentJsonAsync<AdyenModificationResponse>(HttpMethod.Post, $"{VersionSegment}/payments/{pspReference}/captures", request, cancellationToken);

    /// <summary>Cancels a payment before capture (<c>POST /v71/payments/{pspReference}/cancels</c>). Asynchronous; returns a "received" ack.</summary>
    public Task<ApiResult<AdyenModificationResponse>> CancelAsync(string pspReference, AdyenCancelRequest request, CancellationToken cancellationToken = default)
        => SendIdempotentJsonAsync<AdyenModificationResponse>(HttpMethod.Post, $"{VersionSegment}/payments/{pspReference}/cancels", request, cancellationToken);

    /// <summary>Refunds a captured payment (<c>POST /v71/payments/{pspReference}/refunds</c>). Asynchronous; returns a "received" ack.</summary>
    public Task<ApiResult<AdyenModificationResponse>> RefundAsync(string pspReference, AdyenRefundRequest request, CancellationToken cancellationToken = default)
        => SendIdempotentJsonAsync<AdyenModificationResponse>(HttpMethod.Post, $"{VersionSegment}/payments/{pspReference}/refunds", request, cancellationToken);

    private Task<ApiResult<TResponse>> SendIdempotentJsonAsync<TResponse>(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
        if (body is not null)
        {
            request.Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        }

        return SendAsync<TResponse>(request, cancellationToken);
    }
}
