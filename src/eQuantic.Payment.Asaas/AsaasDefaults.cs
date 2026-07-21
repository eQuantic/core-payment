namespace eQuantic.Payment.Asaas;

/// <summary>Constants and conventions for the Asaas provider.</summary>
public static class AsaasDefaults
{
    /// <summary>Provider name used in <c>ProviderInfo</c> (registers as <c>asaas@v3</c>).</summary>
    public const string ProviderName = "asaas";

    /// <summary>Production base URL (already includes the <c>/v3/</c> segment).</summary>
    public const string BaseUrl = "https://api.asaas.com/v3/";

    /// <summary>Sandbox base URL; set it via <see cref="AsaasOptions.BaseUrl"/> to use the sandbox environment.</summary>
    public const string SandboxBaseUrl = "https://api-sandbox.asaas.com/v3/";

    /// <summary>Value sent in the mandatory <c>User-Agent</c> header.</summary>
    public const string UserAgent = "eQuantic.Payment";

    /// <summary>Named <c>HttpClient</c> registration key.</summary>
    public const string HttpClientName = "eQuantic.Payment.Asaas.V3";

    /// <summary>Maps the version enum to its wire string (<c>v3</c>).</summary>
    public static string VersionString(AsaasApiVersion version) => version switch
    {
        AsaasApiVersion.V3 => "v3",
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };
}
