using eQuantic.Payment.Models;

namespace eQuantic.Payment.Pagarme.V5.Mapping;

/// <summary>Wire-format conventions shared by the Pagar.me v5 mappers.</summary>
internal static class PagarmeV5Wire
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
}
