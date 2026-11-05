using eQuantic.Payment.Abstractions;
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Stripe;
using eQuantic.Payment.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.Tests;

/// <summary>
/// MB WAY and Multibanco in Stripe's test mode, in an account that takes them (<see cref="StripeEurTestMode"/>), at
/// Endive. The phone numbers and emails are Stripe's test values: an MB WAY number ending in 6 is declined by the
/// customer and one ending in 5 expires unanswered, any other is approved; a Multibanco voucher is issued for any email.
/// </summary>
public class StripeEurTestModeTests
{
    private static IPaymentProvider Stripe()
    {
        var services = new ServiceCollection();
        services.AddPayments(payments => payments.AddStripe(options =>
        {
            options.ApiKey = StripeEurTestMode.SecretKey;
            options.Version = StripeApiVersion.V2026_09_30_Endive;
        }));
        return services.BuildServiceProvider().GetRequiredService<IPaymentProviderFactory>().Get("stripe");
    }

    private static CreateChargeRequest MbWay(string phone) => new()
    {
        Amount = Money.Eur(25.00m),
        Method = PaymentMethodType.MbWay,
        MbWay = new MbWayDetails { PhoneNumber = phone },
    };

    /// <summary>The charge once it has left <c>requires_action</c>, which Stripe's test numbers do at once.</summary>
    private static async Task<Charge> SettledAsync(IPaymentProvider stripe, string chargeId)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var read = await stripe.Charges.GetAsync(chargeId);
            Assert.True(read.Success, read.Error?.Message);
            if (read.Data!.Status != PaymentStatus.Pending)
            {
                return read.Data;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"{chargeId} was still pending after 20 seconds.");
    }

    [StripeEurTestModeFact]
    public async Task An_MB_WAY_payment_the_customer_approves_is_paid()
    {
        var stripe = Stripe();

        var created = await stripe.Charges.CreateAsync(MbWay("+351 911 111 111"));

        Assert.True(created.Success, created.Error?.Message);
        Assert.Equal(PaymentMethodType.MbWay, created.Data!.Method);
        Assert.Equal(PaymentStatus.Paid, (await SettledAsync(stripe, created.Data.Id)).Status);
    }

    [StripeEurTestModeFact]
    public async Task An_MB_WAY_payment_the_customer_declines_fails()
    {
        var stripe = Stripe();

        var created = await stripe.Charges.CreateAsync(MbWay("911 111 116"));

        Assert.True(created.Success, created.Error?.Message);
        Assert.Equal(PaymentStatus.Failed, (await SettledAsync(stripe, created.Data!.Id)).Status);
    }

    [StripeEurTestModeFact]
    public async Task An_MB_WAY_payment_left_unanswered_expires()
    {
        var stripe = Stripe();

        var created = await stripe.Charges.CreateAsync(MbWay("+351911111115"));

        Assert.True(created.Success, created.Error?.Message);
        Assert.Equal(PaymentStatus.Expired, (await SettledAsync(stripe, created.Data!.Id)).Status);
    }

    [StripeEurTestModeFact]
    public async Task A_Multibanco_voucher_is_issued_with_its_entity_reference_and_page()
    {
        var response = await Stripe().Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Eur(99.00m),
            Method = PaymentMethodType.Multibanco,
            Customer = StripeIntents.Portuguese(),
        });

        Assert.True(response.Success, response.Error?.Message);
        Assert.Equal(PaymentStatus.Pending, response.Data!.Status);
        var voucher = response.Data.Multibanco!;
        Assert.False(string.IsNullOrEmpty(voucher.Entity));
        Assert.False(string.IsNullOrEmpty(voucher.Reference));
        Assert.True(voucher.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.StartsWith("https://", voucher.HostedUrl);
    }
}
