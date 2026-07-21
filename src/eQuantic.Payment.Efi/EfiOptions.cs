using System.Security.Cryptography.X509Certificates;

namespace eQuantic.Payment.Efi;

/// <summary>
/// Efí API products, modeled as versions. They are separate APIs with different hosts, auth and money formats:
/// <see cref="Pix"/> (Central Bank Pix, requires an mTLS client certificate) handles PIX; <see cref="Cobrancas"/>
/// handles boleto and credit card.
/// </summary>
public enum EfiApiVersion
{
    /// <summary>Pix API (<c>pix.api.efipay.com.br</c>) — PIX only, OAuth2 + mTLS.</summary>
    Pix,

    /// <summary>Cobranças API (<c>cobrancas.api.efipay.com.br</c>) — boleto and credit card, OAuth2 (no cert).</summary>
    Cobrancas,
}

public sealed class EfiOptions
{
    /// <summary>OAuth2 client id (per-product credentials in the Efí dashboard).</summary>
    public required string ClientId { get; set; }

    /// <summary>OAuth2 client secret.</summary>
    public required string ClientSecret { get; set; }

    public EfiApiVersion Version { get; set; } = EfiApiVersion.Pix;

    /// <summary>Use the homologação (sandbox) hosts (<c>*-h.api.efipay.com.br</c>).</summary>
    public bool Sandbox { get; set; }

    /// <summary>Override the base URL. Defaults to the host of the selected version/environment.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// mTLS client certificate — REQUIRED for the Pix API (attached on every call, including the token call).
    /// Not used by the Cobranças API. Exported from Efí without a password.
    /// </summary>
    public X509Certificate2? Certificate { get; set; }

    /// <summary>Receiver Pix key (<c>chave</c>) used when creating PIX charges. Required for the Pix API.</summary>
    public string? PixKey { get; set; }
}
