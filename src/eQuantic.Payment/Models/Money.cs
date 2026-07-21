namespace eQuantic.Payment.Models;

/// <summary>
/// Monetary amount expressed in the smallest currency unit (cents/centavos),
/// the format used by every payment gateway API.
/// </summary>
public readonly record struct Money(long AmountInCents, string Currency = Money.DefaultCurrency)
{
    public const string DefaultCurrency = "BRL";

    /// <summary>Decimal representation (e.g. 1050 cents = 10.50).</summary>
    public decimal Amount => AmountInCents / 100m;

    public static Money FromCents(long amountInCents, string currency = DefaultCurrency)
        => new(amountInCents, currency);

    public static Money FromDecimal(decimal amount, string currency = DefaultCurrency)
        => new((long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero), currency);

    /// <summary>Creates an amount in Brazilian Reais (BRL).</summary>
    public static Money Brl(decimal amount) => FromDecimal(amount);

    public override string ToString() => $"{Currency} {Amount:0.00}";
}
