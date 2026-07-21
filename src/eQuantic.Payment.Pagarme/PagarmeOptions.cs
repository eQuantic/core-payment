namespace eQuantic.Payment.Pagarme;

/// <summary>
/// Pagar.me API versions. Each version has a different surface:
/// v4 works with <c>transactions</c>, v5 (Core API) works with <c>orders</c> + <c>charges</c>.
/// </summary>
public enum PagarmeApiVersion
{
    /// <summary>Legacy API (<c>https://api.pagar.me/1</c>) — transaction-based, <c>api_key</c> auth.</summary>
    V4,

    /// <summary>Core API v5 (<c>https://api.pagar.me/core/v5</c>) — order/charge-based, Basic auth with secret key.</summary>
    V5,
}

public sealed class PagarmeOptions
{
    /// <summary>Secret key: <c>sk_xxx</c> for v5, <c>ak_xxx</c> for v4.</summary>
    public required string ApiKey { get; set; }

    public PagarmeApiVersion Version { get; set; } = PagarmeApiVersion.V5;

    /// <summary>Override the base URL (sandbox/proxy). Defaults to the official endpoint of the selected version.</summary>
    public string? BaseUrl { get; set; }
}
