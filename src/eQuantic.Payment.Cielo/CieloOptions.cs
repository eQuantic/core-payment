namespace eQuantic.Payment.Cielo;

/// <summary>
/// Cielo E-commerce API surfaces. Only the modern JSON API 3.0 is implemented; the legacy
/// Webservice 1.5 (XML over a single <c>.do</c> endpoint) is a separate, incompatible version axis, not modeled.
/// </summary>
public enum CieloApiVersion
{
    /// <summary>API E-commerce 3.0 (REST + JSON, <c>/1/sales</c>).</summary>
    V3,
}

/// <summary>Configuration for the Cielo provider.</summary>
public sealed class CieloOptions
{
    /// <summary>Store identifier at Cielo (GUID). Sent as the <c>MerchantId</c> header.</summary>
    public required string MerchantId { get; set; }

    /// <summary>Public key for dual authentication. Sent as the <c>MerchantKey</c> header.</summary>
    public required string MerchantKey { get; set; }

    /// <summary>API version to target. Only <see cref="CieloApiVersion.V3"/> is supported.</summary>
    public CieloApiVersion Version { get; set; } = CieloApiVersion.V3;

    /// <summary>
    /// Override the transactional base URL used for create/capture/void (POST/PUT). Sandbox is
    /// <c>https://apisandbox.cieloecommerce.cielo.com.br/</c>. Defaults to production.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// Override the query base URL used for consult (GET). Sandbox is
    /// <c>https://apiquerysandbox.cieloecommerce.cielo.com.br/</c>. Defaults to production.
    /// </summary>
    public string? QueryBaseUrl { get; set; }
}
