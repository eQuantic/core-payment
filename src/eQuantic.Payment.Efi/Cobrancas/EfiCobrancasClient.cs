using System.Text.Json;
using eQuantic.Payment.Http;
using eQuantic.Payment.Efi.Cobrancas.Models;

namespace eQuantic.Payment.Efi.Cobrancas;

/// <summary>
/// Typed client for the Efí Cobranças API (<c>/v1/charge</c>) — boleto and credit card.
/// snake_case JSON; OAuth Bearer (injected per request); no client certificate.
/// </summary>
public class EfiCobrancasClient(HttpClient httpClient, EfiTokenProvider tokenProvider) : EfiClientBase(httpClient, tokenProvider)
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateOptions(JsonNamingPolicy.SnakeCaseLower);

    protected override JsonSerializerOptions JsonOptions => SerializerOptions;

    public Task<ApiResult<EfiOneStepResponse>> CreateOneStepAsync(EfiOneStepRequest request, CancellationToken cancellationToken = default)
        => SendAuthedAsync<EfiOneStepResponse>(HttpMethod.Post, "v1/charge/one-step", request, cancellationToken);

    public Task<ApiResult<EfiOneStepResponse>> GetChargeAsync(string chargeId, CancellationToken cancellationToken = default)
        => SendAuthedAsync<EfiOneStepResponse>(HttpMethod.Get, $"v1/charge/{chargeId}", null, cancellationToken);

    public Task<ApiResult<EfiOneStepResponse>> CancelChargeAsync(string chargeId, CancellationToken cancellationToken = default)
        => SendAuthedAsync<EfiOneStepResponse>(HttpMethod.Put, $"v1/charge/{chargeId}/cancel", null, cancellationToken);

    public Task<ApiResult<EfiOneStepResponse>> RefundChargeAsync(string chargeId, CancellationToken cancellationToken = default)
        => SendAuthedAsync<EfiOneStepResponse>(HttpMethod.Put, $"v1/charge/{chargeId}/refund", null, cancellationToken);
}
