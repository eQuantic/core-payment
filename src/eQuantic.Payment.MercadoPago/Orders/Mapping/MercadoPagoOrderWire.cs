using System.Globalization;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.MercadoPago.Orders.Mapping;

/// <summary>Value conversions for the Orders API, whose amounts are strings and methods carry an id + type.</summary>
internal static class MercadoPagoOrderWire
{
    /// <summary>Formats a decimal amount as the Orders API string form (e.g. <c>"150.00"</c>, invariant culture).</summary>
    public static string ToAmount(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    public static decimal ParseAmount(string? amount)
        => decimal.TryParse(amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : 0m;

    /// <summary>Resolves the Orders <c>payment_method</c> <c>id</c> and <c>type</c> for a unified method.</summary>
    public static (string Id, string Type) ToPaymentMethod(PaymentMethodType method, string? cardBrand) => method switch
    {
        PaymentMethodType.Pix => ("pix", "bank_transfer"),
        PaymentMethodType.Boleto => ("bolbradesco", "ticket"),
        PaymentMethodType.CreditCard => (cardBrand ?? string.Empty, "credit_card"),
        PaymentMethodType.DebitCard => (cardBrand ?? string.Empty, "debit_card"),
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };

    public static PaymentMethodType FromType(string? type) => type?.ToLowerInvariant() switch
    {
        "bank_transfer" => PaymentMethodType.Pix,
        "ticket" => PaymentMethodType.Boleto,
        "credit_card" => PaymentMethodType.CreditCard,
        "debit_card" => PaymentMethodType.DebitCard,
        _ => PaymentMethodType.CreditCard,
    };
}
