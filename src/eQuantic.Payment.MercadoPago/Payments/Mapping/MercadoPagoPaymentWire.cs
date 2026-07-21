using eQuantic.Payment.Models;

namespace eQuantic.Payment.MercadoPago.Payments.Mapping;

/// <summary>Payment-method conversions shared by the Payments API mappers.</summary>
internal static class MercadoPagoPaymentWire
{
    /// <summary>
    /// Resolves the Mercado Pago <c>payment_method_id</c>. PIX and boleto are fixed; cards use the
    /// caller-supplied brand (e.g. <c>visa</c>, <c>debvisa</c>) since MP requires the specific brand id.
    /// </summary>
    public static string ToPaymentMethodId(PaymentMethodType method, string? cardBrand) => method switch
    {
        PaymentMethodType.Pix => "pix",
        PaymentMethodType.Boleto => "bolbradesco",
        PaymentMethodType.CreditCard or PaymentMethodType.DebitCard => cardBrand ?? string.Empty,
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };

    public static PaymentMethodType FromPaymentTypeId(string? paymentTypeId) => paymentTypeId?.ToLowerInvariant() switch
    {
        "bank_transfer" => PaymentMethodType.Pix,
        "ticket" => PaymentMethodType.Boleto,
        "credit_card" => PaymentMethodType.CreditCard,
        "debit_card" => PaymentMethodType.DebitCard,
        _ => PaymentMethodType.CreditCard,
    };
}
