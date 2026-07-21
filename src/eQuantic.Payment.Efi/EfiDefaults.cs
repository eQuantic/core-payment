namespace eQuantic.Payment.Efi;

public static class EfiDefaults
{
    public const string ProviderName = "efi";

    public const string PixBaseUrl = "https://pix.api.efipay.com.br/";
    public const string PixSandboxBaseUrl = "https://pix-h.api.efipay.com.br/";
    public const string CobrancasBaseUrl = "https://cobrancas.api.efipay.com.br/";
    public const string CobrancasSandboxBaseUrl = "https://cobrancas-h.api.efipay.com.br/";

    public const string PixTokenPath = "oauth/token";
    public const string CobrancasTokenPath = "v1/authorize";

    // Named HttpClients: one for the token endpoint, one for the API (both carry the mTLS cert for Pix).
    public const string PixTokenHttpClientName = "eQuantic.Payment.Efi.Pix.Token";
    public const string PixApiHttpClientName = "eQuantic.Payment.Efi.Pix.Api";
    public const string CobrancasTokenHttpClientName = "eQuantic.Payment.Efi.Cobrancas.Token";
    public const string CobrancasApiHttpClientName = "eQuantic.Payment.Efi.Cobrancas.Api";

    public static string VersionString(EfiApiVersion version) => version switch
    {
        EfiApiVersion.Pix => "pix",
        EfiApiVersion.Cobrancas => "cobrancas",
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    public static string BaseUrlFor(EfiApiVersion version, bool sandbox) => (version, sandbox) switch
    {
        (EfiApiVersion.Pix, false) => PixBaseUrl,
        (EfiApiVersion.Pix, true) => PixSandboxBaseUrl,
        (EfiApiVersion.Cobrancas, false) => CobrancasBaseUrl,
        (EfiApiVersion.Cobrancas, true) => CobrancasSandboxBaseUrl,
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };
}
