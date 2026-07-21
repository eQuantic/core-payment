using eQuantic.Payment.Models;

namespace eQuantic.Payment.Pagarme.V4.Mapping;

/// <summary>Wire-format conventions shared by the Pagar.me v4 (legacy) mappers.</summary>
internal static class PagarmeV4Wire
{
    public static string ToPaymentMethod(PaymentMethodType method) => method switch
    {
        PaymentMethodType.CreditCard => "credit_card",
        PaymentMethodType.DebitCard => "debit_card",
        PaymentMethodType.Pix => "pix",
        PaymentMethodType.Boleto => "boleto",
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };

    public static PaymentMethodType FromPaymentMethod(string? method) => method?.ToLowerInvariant() switch
    {
        "credit_card" => PaymentMethodType.CreditCard,
        "debit_card" => PaymentMethodType.DebitCard,
        "pix" => PaymentMethodType.Pix,
        "boleto" => PaymentMethodType.Boleto,
        _ => PaymentMethodType.CreditCard,
    };

    /// <summary>Formats an expiration as the legacy <c>MMYY</c> string.</summary>
    public static string ToExpirationDate(int month, int year) => $"{month:D2}{year % 100:D2}";
}
