namespace eQuantic.Payment.MercadoPago;

public static class MercadoPagoDefaults
{
    public const string ProviderName = "mercadopago";

    public const string BaseUrl = "https://api.mercadopago.com/";

    public const string PaymentsHttpClientName = "eQuantic.Payment.MercadoPago.Payments";
    public const string OrdersHttpClientName = "eQuantic.Payment.MercadoPago.Orders";

    public static string VersionString(MercadoPagoApiVersion version) => version switch
    {
        MercadoPagoApiVersion.Payments => "payments",
        MercadoPagoApiVersion.Orders => "orders",
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };
}
