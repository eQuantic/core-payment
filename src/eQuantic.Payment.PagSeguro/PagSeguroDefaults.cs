namespace eQuantic.Payment.PagSeguro;

public static class PagSeguroDefaults
{
    public const string ProviderName = "pagseguro";

    public const string BaseUrl = "https://api.pagseguro.com/";

    public const string OrdersHttpClientName = "eQuantic.Payment.PagSeguro.Orders";

    public static string VersionString(PagSeguroApiVersion version) => version switch
    {
        PagSeguroApiVersion.Orders => "orders",
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };
}
