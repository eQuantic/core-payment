using eQuantic.Payment.Models;

namespace eQuantic.Payment.Tests;

public class MoneyTests
{
    [Theory]
    [InlineData(10.00, 1000)]
    [InlineData(10.50, 1050)]
    [InlineData(0.99, 99)]
    [InlineData(1234.56, 123456)]
    public void FromDecimal_converts_to_cents(decimal amount, long expectedCents)
    {
        Assert.Equal(expectedCents, Money.FromDecimal(amount).AmountInCents);
    }

    [Fact]
    public void FromDecimal_rounds_half_away_from_zero()
    {
        Assert.Equal(1006, Money.FromDecimal(10.055m).AmountInCents);
    }

    [Fact]
    public void Brl_is_the_default_currency()
    {
        Assert.Equal("BRL", Money.Brl(10m).Currency);
    }

    [Fact]
    public void Amount_reconstructs_the_decimal()
    {
        Assert.Equal(10.50m, Money.FromCents(1050).Amount);
    }
}
