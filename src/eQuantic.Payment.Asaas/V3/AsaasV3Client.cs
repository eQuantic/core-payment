using System.Text.Json;
using System.Text.Json.Serialization;
using eQuantic.Payment.Asaas.V3.Models;
using eQuantic.Payment.Http;

namespace eQuantic.Payment.Asaas.V3;

/// <summary>
/// Typed client for the Asaas v3 API (payments and customers). JSON wire format with camelCase keys; the
/// <c>access_token</c> and <c>User-Agent</c> headers are configured on the injected <see cref="HttpClient"/>.
/// All GET requests are sent with no body (Asaas rejects GETs that carry one with HTTP 403).
/// </summary>
public class AsaasV3Client(HttpClient httpClient) : PaymentHttpClientBase(httpClient)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    protected override JsonSerializerOptions JsonOptions => SerializerOptions;

    public Task<ApiResult<AsaasCustomerResponse>> CreateCustomerAsync(AsaasCustomerRequest request, CancellationToken cancellationToken = default)
        => SendJsonAsync<AsaasCustomerResponse>(HttpMethod.Post, "customers", request, cancellationToken);

    public Task<ApiResult<AsaasCustomerResponse>> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        => SendJsonAsync<AsaasCustomerResponse>(HttpMethod.Get, $"customers/{customerId}", null, cancellationToken);

    public Task<ApiResult<AsaasPaymentResponse>> CreatePaymentAsync(AsaasPaymentRequest request, CancellationToken cancellationToken = default)
        => SendJsonAsync<AsaasPaymentResponse>(HttpMethod.Post, "payments", request, cancellationToken);

    public Task<ApiResult<AsaasPaymentResponse>> GetPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
        => SendJsonAsync<AsaasPaymentResponse>(HttpMethod.Get, $"payments/{paymentId}", null, cancellationToken);

    public Task<ApiResult<AsaasDeleteResponse>> DeletePaymentAsync(string paymentId, CancellationToken cancellationToken = default)
        => SendJsonAsync<AsaasDeleteResponse>(HttpMethod.Delete, $"payments/{paymentId}", null, cancellationToken);

    public Task<ApiResult<AsaasPaymentResponse>> CaptureAuthorizedPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
        => SendJsonAsync<AsaasPaymentResponse>(HttpMethod.Post, $"payments/{paymentId}/captureAuthorizedPayment", null, cancellationToken);

    public Task<ApiResult<AsaasPaymentResponse>> RefundPaymentAsync(string paymentId, AsaasRefundRequest? request, CancellationToken cancellationToken = default)
        => SendJsonAsync<AsaasPaymentResponse>(HttpMethod.Post, $"payments/{paymentId}/refund", request, cancellationToken);

    public Task<ApiResult<AsaasPixQrCodeResponse>> GetPixQrCodeAsync(string paymentId, CancellationToken cancellationToken = default)
        => SendJsonAsync<AsaasPixQrCodeResponse>(HttpMethod.Get, $"payments/{paymentId}/pixQrCode", null, cancellationToken);

    public Task<ApiResult<AsaasIdentificationFieldResponse>> GetIdentificationFieldAsync(string paymentId, CancellationToken cancellationToken = default)
        => SendJsonAsync<AsaasIdentificationFieldResponse>(HttpMethod.Get, $"payments/{paymentId}/identificationField", null, cancellationToken);
}
