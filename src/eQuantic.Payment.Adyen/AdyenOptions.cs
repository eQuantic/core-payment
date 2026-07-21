namespace eQuantic.Payment.Adyen;

/// <summary>
/// Adyen Checkout API versions. The version is a URL path segment (<c>/v71/payments</c>); a single
/// integration pins one version for every endpoint. Only the current stable <see cref="V71"/> is modeled.
/// </summary>
public enum AdyenApiVersion
{
    /// <summary>Checkout API <c>v71</c> (<c>/v71/payments</c>, <c>/v71/payments/{pspReference}/captures</c>, ...).</summary>
    V71,
}

/// <summary>Configuration for the Adyen Checkout provider.</summary>
public sealed class AdyenOptions
{
    /// <summary>API key sent in the <c>X-API-Key</c> header (Customer Area, per environment).</summary>
    public required string ApiKey { get; set; }

    /// <summary>
    /// Adyen merchant account name. Sent as the <c>merchantAccount</c> body field on every request
    /// (it is a body field, not an authentication mechanism).
    /// </summary>
    public required string MerchantAccount { get; set; }

    /// <summary>Checkout API version pinned for every endpoint. Defaults to <see cref="AdyenApiVersion.V71"/>.</summary>
    public AdyenApiVersion Version { get; set; } = AdyenApiVersion.V71;

    /// <summary>
    /// Override the base URL. Defaults to the test endpoint <c>https://checkout-test.adyen.com/</c>.
    /// The live endpoint differs and includes a company prefix and a <c>/checkout/</c> segment
    /// (e.g. <c>https://{PREFIX}-checkout-live.adyenpayments.com/checkout/</c>); the <c>v71/...</c>
    /// path is appended to whatever base URL is configured.
    /// </summary>
    public string? BaseUrl { get; set; }
}
