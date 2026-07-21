using System.Text.Json;
using eQuantic.Payment.Http;
using eQuantic.Payment.Efi.Pix.Models;

namespace eQuantic.Payment.Efi.Pix;

/// <summary>
/// Typed client for the Efí Pix API (<c>/v2/cob</c>, <c>/v2/loc</c>, <c>/v2/pix</c>).
/// camelCase JSON; OAuth Bearer (injected per request) + mTLS certificate on the underlying handler.
/// </summary>
public class EfiPixClient(HttpClient httpClient, EfiTokenProvider tokenProvider) : EfiClientBase(httpClient, tokenProvider)
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateOptions(JsonNamingPolicy.CamelCase);

    protected override JsonSerializerOptions JsonOptions => SerializerOptions;

    public Task<ApiResult<EfiPixCobResponse>> CreateChargeAsync(string txid, EfiPixCobRequest request, CancellationToken cancellationToken = default)
        => SendAuthedAsync<EfiPixCobResponse>(HttpMethod.Put, $"v2/cob/{txid}", request, cancellationToken);

    public Task<ApiResult<EfiPixCobResponse>> GetChargeAsync(string txid, CancellationToken cancellationToken = default)
        => SendAuthedAsync<EfiPixCobResponse>(HttpMethod.Get, $"v2/cob/{txid}", null, cancellationToken);

    public Task<ApiResult<EfiPixQrCodeResponse>> GetQrCodeAsync(long locId, CancellationToken cancellationToken = default)
        => SendAuthedAsync<EfiPixQrCodeResponse>(HttpMethod.Get, $"v2/loc/{locId}/qrcode", null, cancellationToken);

    public Task<ApiResult<EfiPixDevolucao>> CreateRefundAsync(string endToEndId, string refundId, EfiPixDevolucaoRequest request, CancellationToken cancellationToken = default)
        => SendAuthedAsync<EfiPixDevolucao>(HttpMethod.Put, $"v2/pix/{endToEndId}/devolucao/{refundId}", request, cancellationToken);
}
