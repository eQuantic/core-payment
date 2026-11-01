using eQuantic.Payment.Stripe;
using eQuantic.Payment.Stripe.V1;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

/// <summary>
/// What the provider sends at Endive, checked against Stripe's schema for Endive (stripe-mock) rather than against what
/// the other tests expect. A card charged from a token is not here: that schema has no <c>card</c> among
/// <c>payment_method_data[type]</c>'s values, at any version, so Stripe's test mode answers for it instead
/// (<see cref="StripeTestModeTests"/>).
/// </summary>
public class StripeEndiveSchemaTests(StripeMock stripe) : IClassFixture<StripeMock>
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private StripeProviderV1 Provider(StripeApiVersion version) =>
        new(new StripeClientV1(stripe.Http()), version, TestMapperFactory.Create());

    [Theory]
    [InlineData(StripeIntents.SavedCard)]
    [InlineData(StripeIntents.Pix)]
    [InlineData(StripeIntents.Boleto)]
    public async Task Endives_schema_accepts_the_charge_built_at_Endive(string kind)
    {
        var response = await Provider(StripeApiVersion.V2026_09_30_Endive).Charges.CreateAsync(StripeIntents.Charge(kind, Today));

        Assert.True(response.Success, response.Error?.Message);
        Assert.StartsWith("pi_", response.Data!.Id);
    }

    [Fact]
    public async Task Endives_schema_accepts_the_card_setup_built_at_Endive()
    {
        var response = await Provider(StripeApiVersion.V2026_09_30_Endive).PaymentMethods.SetupAsync(StripeIntents.CardSetup());

        Assert.True(response.Success, response.Error?.Message);
        Assert.StartsWith("seti_", response.Data!.Id);
    }

    /// <summary>The check fails where it should: the same charge, built at Basil, is one Endive refuses.</summary>
    [Fact]
    public async Task Endives_schema_refuses_the_charge_built_at_Basil()
    {
        var response = await Provider(StripeApiVersion.V2025_04_30_Basil).Charges.CreateAsync(StripeIntents.Charge(StripeIntents.SavedCard, Today));

        Assert.False(response.Success);
        Assert.Contains("additional properties are not allowed", response.Error!.Message);
    }
}
