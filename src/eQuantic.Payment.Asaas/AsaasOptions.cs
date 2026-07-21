namespace eQuantic.Payment.Asaas;

/// <summary>
/// Asaas API versions. Only v3 (the current REST API) is modeled; the code lives under the <c>V3/</c> folder.
/// </summary>
public enum AsaasApiVersion
{
    /// <summary>Asaas API v3 (<c>https://api.asaas.com/v3/</c>).</summary>
    V3,
}

/// <summary>Configuration for the Asaas provider.</summary>
public sealed class AsaasOptions
{
    /// <summary>
    /// API key from the Asaas dashboard (production keys start with <c>$aact_prod_</c>, sandbox with
    /// <c>$aact_hmlg_</c>). Sent in the custom <c>access_token</c> header (not <c>Authorization: Bearer</c>).
    /// </summary>
    public required string ApiKey { get; set; }

    /// <summary>API version to target. Defaults to <see cref="AsaasApiVersion.V3"/>.</summary>
    public AsaasApiVersion Version { get; set; } = AsaasApiVersion.V3;

    /// <summary>
    /// Override the base URL. Defaults to <c>https://api.asaas.com/v3/</c>; set
    /// <c>https://api-sandbox.asaas.com/v3/</c> (see <see cref="AsaasDefaults.SandboxBaseUrl"/>) for sandbox.
    /// </summary>
    public string? BaseUrl { get; set; }
}
