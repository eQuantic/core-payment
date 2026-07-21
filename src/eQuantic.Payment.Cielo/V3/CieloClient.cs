using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using eQuantic.Payment.Cielo.V3.Models;
using eQuantic.Payment.Http;

namespace eQuantic.Payment.Cielo.V3;

/// <summary>
/// Typed client for the Cielo E-commerce API 3.0 (<c>/1/sales</c>). JSON wire format with PascalCase keys;
/// dual-header auth (<c>MerchantId</c> + <c>MerchantKey</c>) set on the <see cref="HttpClient"/>.
/// Create/capture/void target the transactional host (the client's base address); consult (GET) targets the
/// separate query host via an absolute URI.
/// </summary>
public class CieloClient : PaymentHttpClientBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        // Cielo API 3.0 uses PascalCase keys (MerchantOrderId, Payment, CreditCard, QrCodeString, ...).
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private readonly string _queryBaseUrl;

    /// <summary>Creates the client. <paramref name="queryBaseUrl"/> is the query host used for GET consults.</summary>
    public CieloClient(HttpClient httpClient, string queryBaseUrl) : base(httpClient)
    {
        _queryBaseUrl = queryBaseUrl;
    }

    protected override JsonSerializerOptions JsonOptions => SerializerOptions;

    /// <summary>Creates a sale (<c>POST /1/sales/</c>) on the transactional host.</summary>
    public Task<ApiResult<CieloSaleResponse>> CreateSaleAsync(CieloSaleRequest request, CancellationToken cancellationToken = default)
        => SendJsonAsync<CieloSaleResponse>(HttpMethod.Post, "1/sales/", request, cancellationToken);

    /// <summary>Consults a sale (<c>GET /1/sales/{paymentId}</c>) on the query host (absolute URI).</summary>
    public Task<ApiResult<CieloSaleResponse>> GetSaleAsync(string paymentId, CancellationToken cancellationToken = default)
        => SendJsonAsync<CieloSaleResponse>(HttpMethod.Get, $"{_queryBaseUrl}1/sales/{paymentId}", null, cancellationToken);

    /// <summary>Captures an authorized sale (<c>PUT /1/sales/{paymentId}/capture</c>); <paramref name="amount"/> in the query string for partial capture.</summary>
    public Task<ApiResult<CieloReturnResponse>> CaptureSaleAsync(string paymentId, long? amount = null, CancellationToken cancellationToken = default)
        => SendJsonAsync<CieloReturnResponse>(HttpMethod.Put, $"1/sales/{paymentId}/capture{AmountQuery(amount)}", null, cancellationToken);

    /// <summary>
    /// Voids a sale (<c>PUT /1/sales/{paymentId}/void</c>). The same endpoint cancels an authorized sale and refunds
    /// a captured one; <paramref name="amount"/> in the query string performs a partial void/refund.
    /// </summary>
    public Task<ApiResult<CieloReturnResponse>> VoidSaleAsync(string paymentId, long? amount = null, CancellationToken cancellationToken = default)
        => SendJsonAsync<CieloReturnResponse>(HttpMethod.Put, $"1/sales/{paymentId}/void{AmountQuery(amount)}", null, cancellationToken);

    private static string AmountQuery(long? amount)
        => amount is { } v ? $"?amount={v.ToString(CultureInfo.InvariantCulture)}" : string.Empty;
}
