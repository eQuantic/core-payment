using eQuantic.Payment.MercadoPago.Payments.Models;
using eQuantic.Payment.Models.Requests;

namespace eQuantic.Payment.MercadoPago;

/// <summary>Shared conversions between the unified model and Mercado Pago wire values.</summary>
internal static class MercadoPagoConventions
{
    public static MercadoPagoIdentification? ToIdentification(CustomerRequest customer)
    {
        if (customer.DocumentDigits is not { } digits)
        {
            return null;
        }

        return new MercadoPagoIdentification
        {
            Type = customer.DocumentType == DocumentType.Cnpj ? "CNPJ" : "CPF",
            Number = digits,
        };
    }

    public static MercadoPagoPhone? ToPhone(string? phone)
    {
        var digits = phone is null ? null : new string(phone.Where(char.IsDigit).ToArray());
        if (digits is null || digits.Length < 10)
        {
            return null;
        }

        if (digits.Length > 11 && digits.StartsWith("55"))
        {
            digits = digits[2..];
        }

        return new MercadoPagoPhone { AreaCode = digits[..2], Number = digits[2..] };
    }

    /// <summary>Splits the unified single-field name into Mercado Pago's first/last name.</summary>
    public static (string? First, string? Last) SplitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (null, null);
        }

        var parts = name.Trim().Split(' ', 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (parts[0], null);
    }
}
