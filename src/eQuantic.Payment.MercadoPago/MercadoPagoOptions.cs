namespace eQuantic.Payment.MercadoPago;

/// <summary>
/// Mercado Pago API surfaces. Both create payments but differ in shape (analogous to Pagar.me v4/v5):
/// the <see cref="Payments"/> API (<c>/v1/payments</c>) is flat with numeric ids and decimal amounts;
/// the <see cref="Orders"/> API (<c>/v1/orders</c>) is the newer unified model with string ids,
/// string amounts and a different status vocabulary.
/// </summary>
public enum MercadoPagoApiVersion
{
    /// <summary>Payments API — <c>POST /v1/payments</c> (flat Payment object).</summary>
    Payments,

    /// <summary>Orders API — <c>POST /v1/orders</c> (nested <c>transactions.payments[]</c>).</summary>
    Orders,
}

public sealed class MercadoPagoOptions
{
    /// <summary>Access token: <c>APP_USR-...</c> (production) or <c>TEST-...</c> (sandbox).</summary>
    public required string AccessToken { get; set; }

    public MercadoPagoApiVersion Version { get; set; } = MercadoPagoApiVersion.Payments;

    /// <summary>Override the base URL (proxy/mock). Defaults to <c>https://api.mercadopago.com/</c>.</summary>
    public string? BaseUrl { get; set; }
}
