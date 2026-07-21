namespace eQuantic.Payment.Pagarme;

public static class PagarmeDefaults
{
    public const string ProviderName = "pagarme";

    public const string V4BaseUrl = "https://api.pagar.me/1/";
    public const string V5BaseUrl = "https://api.pagar.me/core/v5/";

    public const string V4HttpClientName = "eQuantic.Payment.Pagarme.V4";
    public const string V5HttpClientName = "eQuantic.Payment.Pagarme.V5";

    public static string VersionString(PagarmeApiVersion version) => version switch
    {
        PagarmeApiVersion.V4 => "v4",
        PagarmeApiVersion.V5 => "v5",
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };
}
