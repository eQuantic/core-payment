namespace eQuantic.Payment.Adyen;

/// <summary>Shared constants and version helpers for the Adyen provider.</summary>
public static class AdyenDefaults
{
    /// <summary>Provider name surfaced in <c>ProviderInfo</c> (registration key <c>adyen@v71</c>).</summary>
    public const string ProviderName = "adyen";

    /// <summary>Default test/sandbox base URL. The live URL differs; override it via <see cref="AdyenOptions.BaseUrl"/>.</summary>
    public const string BaseUrl = "https://checkout-test.adyen.com/";

    /// <summary>Name of the named <see cref="System.Net.Http.HttpClient"/> registered for the v71 API.</summary>
    public const string HttpClientName = "eQuantic.Payment.Adyen.V71";

    /// <summary>The version identifier surfaced in <c>ProviderInfo</c> (e.g. <c>adyen@v71</c>).</summary>
    public static string VersionString(AdyenApiVersion version) => version switch
    {
        AdyenApiVersion.V71 => "v71",
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    /// <summary>The URL path segment prefixing every endpoint for the given version (e.g. <c>v71</c>).</summary>
    public static string VersionPath(AdyenApiVersion version) => VersionString(version);
}
