using System.Net.Http.Headers;
using System.Text.Json;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Stripe;
using eQuantic.Payment.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.Tests;

/// <summary>
/// What stripe-mock cannot tell: whether Stripe itself accepts the intents the provider creates at Endive, confirmed and
/// without a <c>return_url</c>, as it accepted them with <c>payment_method_types</c>; and the card charged from a token,
/// a form Stripe's published schema does not list. Each test makes what it needs in the account's test mode.
/// </summary>
public class StripeTestModeTests
{
    private const StripeApiVersion Endive = StripeApiVersion.V2026_09_30_Endive;

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Stripe pinned to Endive, registered the way a consumer registers it.</summary>
    private static IPaymentProvider Stripe()
    {
        var services = new ServiceCollection();
        services.AddPayments(payments => payments.AddStripe(options =>
        {
            options.ApiKey = StripeTestMode.SecretKey;
            options.Version = Endive;
        }));
        return services.BuildServiceProvider().GetRequiredService<IPaymentProviderFactory>().Get("stripe");
    }

    /// <summary>
    /// What a product's front end does with Stripe.js: confirms the setup with one of Stripe's test cards
    /// (<c>pm_card_visa</c>), which saves the card it produces to the setup's customer.
    /// </summary>
    private static async Task ConfirmSetupAsync(string setupId)
    {
        using var http = new HttpClient { BaseAddress = new Uri(StripeDefaults.BaseUrl) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", StripeTestMode.SecretKey);
        http.DefaultRequestHeaders.Add("Stripe-Version", StripeDefaults.StripeVersionHeader(Endive));

        using var response = await http.PostAsync(
            $"setup_intents/{setupId}/confirm", new FormUrlEncodedContent([KeyValuePair.Create("payment_method", "pm_card_visa")]));
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, body);
        Assert.Equal("succeeded", JsonDocument.Parse(body).RootElement.GetProperty("status").GetString());
    }

    [StripeTestModeFact]
    public async Task A_card_charged_from_a_token_is_paid()
    {
        var response = await Stripe().Charges.CreateAsync(StripeIntents.Charge(StripeIntents.Card, Today));

        Assert.True(response.Success, response.Error?.Message);
        Assert.Equal("2026-09-30.endive", response.Provider.Version);
        Assert.Equal(PaymentStatus.Paid, response.Data!.Status);
    }

    [StripeTestModeFact]
    public async Task A_card_saved_through_a_setup_is_charged_with_the_customer_away()
    {
        var stripe = Stripe();
        var customer = await stripe.Customers.CreateAsync(new CustomerRequest { Name = "Maria Silva", Email = "maria.silva@example.com" });
        Assert.True(customer.Success, customer.Error?.Message);

        var setup = await stripe.PaymentMethods.SetupAsync(StripeIntents.CardSetup(customer.Data!.Id));
        Assert.True(setup.Success, setup.Error?.Message);
        await ConfirmSetupAsync(setup.Data!.Id);
        var saved = await stripe.PaymentMethods.GetSetupAsync(setup.Data.Id);
        Assert.True(saved.Success, saved.Error?.Message);

        var charge = await stripe.Charges.CreateAsync(
            StripeIntents.Charge(StripeIntents.SavedCard, Today, customer.Data.Id, saved.Data!.PaymentMethodId!));

        Assert.True(charge.Success, charge.Error?.Message);
        Assert.Equal(PaymentStatus.Paid, charge.Data!.Status);
    }

    [StripeTestModeFact]
    public async Task A_boleto_is_issued_with_its_voucher()
    {
        var response = await Stripe().Charges.CreateAsync(StripeIntents.Charge(StripeIntents.Boleto, Today));

        Assert.True(response.Success, response.Error?.Message);
        Assert.Equal(PaymentStatus.Pending, response.Data!.Status);
        Assert.False(string.IsNullOrEmpty(response.Data.Boleto?.DigitableLine ?? response.Data.Boleto?.Barcode));
    }
}
