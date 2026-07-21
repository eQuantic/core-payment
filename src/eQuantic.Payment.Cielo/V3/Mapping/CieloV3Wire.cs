using eQuantic.Payment.Models;

namespace eQuantic.Payment.Cielo.V3.Mapping;

/// <summary>Wire-format conventions shared by the Cielo API 3.0 mappers.</summary>
internal static class CieloV3Wire
{
    /// <summary>Maps a unified payment method to the Cielo <c>Payment.Type</c> string.</summary>
    public static string ToPaymentType(PaymentMethodType method) => method switch
    {
        PaymentMethodType.CreditCard => "CreditCard",
        PaymentMethodType.DebitCard => "DebitCard",
        PaymentMethodType.Pix => "Pix",
        PaymentMethodType.Boleto => "Boleto",
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };

    /// <summary>Maps a Cielo <c>Payment.Type</c> string to the unified payment method (defaults to credit card).</summary>
    public static PaymentMethodType FromPaymentType(string? type) => type?.ToUpperInvariant() switch
    {
        "CREDITCARD" => PaymentMethodType.CreditCard,
        "DEBITCARD" => PaymentMethodType.DebitCard,
        "PIX" => PaymentMethodType.Pix,
        "BOLETO" => PaymentMethodType.Boleto,
        _ => PaymentMethodType.CreditCard,
    };
}
