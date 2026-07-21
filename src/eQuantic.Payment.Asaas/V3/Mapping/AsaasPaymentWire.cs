using System.Globalization;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Asaas.V3.Mapping;

/// <summary>Billing-type and date conversions shared by the Asaas v3 mappers.</summary>
internal static class AsaasPaymentWire
{
    /// <summary>
    /// Resolves the Asaas <c>billingType</c>. Both card methods map to <c>CREDIT_CARD</c> because Asaas has no
    /// separate debit billing type for standard charges.
    /// </summary>
    public static string ToBillingType(PaymentMethodType method) => method switch
    {
        PaymentMethodType.Pix => "PIX",
        PaymentMethodType.Boleto => "BOLETO",
        PaymentMethodType.CreditCard or PaymentMethodType.DebitCard => "CREDIT_CARD",
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };

    public static PaymentMethodType FromBillingType(string? billingType) => billingType?.ToUpperInvariant() switch
    {
        "PIX" => PaymentMethodType.Pix,
        "BOLETO" => PaymentMethodType.Boleto,
        "CREDIT_CARD" => PaymentMethodType.CreditCard,
        "DEBIT_CARD" => PaymentMethodType.DebitCard,
        _ => PaymentMethodType.CreditCard,
    };

    /// <summary>Parses an Asaas date or date-time string; unparseable/empty values return <c>null</c>.</summary>
    public static DateTimeOffset? ParseDateTime(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var result)
            ? result
            : null;

    /// <summary>Parses an Asaas <c>yyyy-MM-dd</c> date; unparseable/empty values return <c>null</c>.</summary>
    public static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? result
            : null;
}
