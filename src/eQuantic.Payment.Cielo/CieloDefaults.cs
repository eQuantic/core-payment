namespace eQuantic.Payment.Cielo;

/// <summary>Default endpoints, provider name and version strings for the Cielo provider.</summary>
public static class CieloDefaults
{
    /// <summary>Provider name used in <c>ProviderInfo</c> (registration key <c>cielo@3.0</c>).</summary>
    public const string ProviderName = "cielo";

    /// <summary>Production transactional host (create/capture/void via POST/PUT).</summary>
    public const string BaseUrl = "https://api.cieloecommerce.cielo.com.br/";

    /// <summary>Production query host (consult via GET).</summary>
    public const string QueryBaseUrl = "https://apiquery.cieloecommerce.cielo.com.br/";

    /// <summary>Named <c>HttpClient</c> registration key for the API 3.0 client.</summary>
    public const string V3HttpClientName = "eQuantic.Payment.Cielo.V3";

    /// <summary>Maps a <see cref="CieloApiVersion"/> to its wire version string.</summary>
    public static string VersionString(CieloApiVersion version) => version switch
    {
        CieloApiVersion.V3 => "3.0",
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };
}
