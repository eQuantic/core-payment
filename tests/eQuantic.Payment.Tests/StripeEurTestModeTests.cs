using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
/// Basil, the default, and at Endive. The phone numbers are Stripe's test values: one ending in 6 is declined by the
/// customer, one ending in 5 expires unanswered, and any other is approved at once.
/// </summary>
public class StripeEurTestModeTests
{
    private const string WebhookSecret = "whsec_eur_test_mode";

    private static IPaymentProvider Stripe(StripeApiVersion version)
    {
        var services = new ServiceCollection();
        services.AddPayments(payments => payments.AddStripe(options =>
        {
            options.ApiKey = StripeEurTestMode.SecretKey;
            options.Version = version;
            options.WebhookSecret = WebhookSecret;
        }));
        return services.BuildServiceProvider().GetRequiredService<IPaymentProviderFactory>().Get("stripe");
    }

    private static CreateChargeRequest MbWay(string phone, string reference) => new()
    {
        Amount = Money.Eur(12.90m),
        Method = PaymentMethodType.MbWay,
        MbWay = new MbWayDetails { PhoneNumber = phone },
        ReferenceId = reference,
    };

    /// <summary>The charge once it has left <c>requires_action</c>, which Stripe's test numbers do within seconds.</summary>
    private static async Task<Charge> SettledAsync(IPaymentProvider stripe, string chargeId)
    {
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var read = await stripe.Charges.GetAsync(chargeId);
            Assert.True(read.Success, read.Error?.Message);
            if (read.Data!.Status != PaymentStatus.Pending)
            {
                return read.Data;
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"{chargeId} was still pending after 30 seconds.");
    }

    /// <summary>
    /// The event Stripe generated of <paramref name="type"/> about <paramref name="paymentIntentId"/>, as its raw JSON:
    /// the body a webhook endpoint receives for it.
    /// </summary>
    private static async Task<string> EventAsync(string type, string paymentIntentId)
    {
        using var http = new HttpClient { BaseAddress = new Uri(StripeDefaults.BaseUrl) };
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", StripeEurTestMode.SecretKey);

        for (var attempt = 0; attempt < 30; attempt++)
        {
            using var document = JsonDocument.Parse(await http.GetStringAsync($"events?type={type}&limit=20"));
            foreach (var stripeEvent in document.RootElement.GetProperty("data").EnumerateArray())
            {
                if (stripeEvent.GetProperty("data").GetProperty("object").GetProperty("id").GetString() == paymentIntentId)
                {
                    return stripeEvent.GetRawText();
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new TimeoutException($"No {type} event about {paymentIntentId} after 30 seconds.");
    }

    /// <summary><paramref name="body"/> as Stripe would deliver it to an endpoint signed with <see cref="WebhookSecret"/>.</summary>
    private static NotificationRequest Signed(string body)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(WebhookSecret), Encoding.UTF8.GetBytes($"{timestamp}.{body}")));
        return new NotificationRequest
        {
            Body = body,
            Headers = new Dictionary<string, string> { [StripeNotificationOperations.SignatureHeader] = $"t={timestamp},v1={signature}" },
        };
    }

    [StripeEurTestModeTheory]
    [InlineData(StripeApiVersion.V2025_04_30_Basil)]
    [InlineData(StripeApiVersion.V2026_09_30_Endive)]
    public async Task An_MB_WAY_payment_the_customer_approves_is_paid_and_its_event_carries_the_charge(StripeApiVersion version)
    {
        var stripe = Stripe(version);
        var reference = $"order-{Guid.NewGuid():N}";

        var created = await stripe.Charges.CreateAsync(MbWay("+351 911 111 111", reference));

        Assert.True(created.Success, created.Error?.Message);
        Assert.Equal(PaymentMethodType.MbWay, created.Data!.Method);
        Assert.Equal(PaymentStatus.Paid, (await SettledAsync(stripe, created.Data.Id)).Status);

        var notification = stripe.Notifications.Verify(Signed(await EventAsync("payment_intent.succeeded", created.Data.Id)));

        Assert.True(notification.Success, notification.Error?.Message);
        var charge = notification.Data!.Charge!;
        Assert.Equal(created.Data.Id, charge.Id);
        Assert.Equal(PaymentMethodType.MbWay, charge.Method);
        Assert.Equal(PaymentStatus.Paid, charge.Status);
        Assert.Equal(Money.Eur(12.90m), charge.Amount);
        Assert.Equal(reference, charge.ReferenceId);
    }

    [StripeEurTestModeTheory]
    [InlineData(StripeApiVersion.V2025_04_30_Basil)]
    [InlineData(StripeApiVersion.V2026_09_30_Endive)]
    public async Task An_MB_WAY_payment_the_customer_declines_fails_and_its_event_says_so(StripeApiVersion version)
    {
        var stripe = Stripe(version);

        var created = await stripe.Charges.CreateAsync(MbWay("911 111 116", $"order-{Guid.NewGuid():N}"));

        Assert.True(created.Success, created.Error?.Message);
        Assert.Equal(PaymentStatus.Failed, (await SettledAsync(stripe, created.Data!.Id)).Status);

        var notification = stripe.Notifications.Verify(Signed(await EventAsync("payment_intent.payment_failed", created.Data.Id)));

        Assert.True(notification.Success, notification.Error?.Message);
        Assert.Equal(PaymentStatus.Failed, notification.Data!.Charge!.Status);
        Assert.Equal(PaymentMethodType.MbWay, notification.Data.Charge.Method);
    }

    [StripeEurTestModeTheory]
    [InlineData(StripeApiVersion.V2025_04_30_Basil)]
    [InlineData(StripeApiVersion.V2026_09_30_Endive)]
    public async Task An_MB_WAY_payment_left_unanswered_expires(StripeApiVersion version)
    {
        var stripe = Stripe(version);

        var created = await stripe.Charges.CreateAsync(MbWay("+351911111115", $"order-{Guid.NewGuid():N}"));

        Assert.True(created.Success, created.Error?.Message);
        Assert.Equal(PaymentStatus.Expired, (await SettledAsync(stripe, created.Data!.Id)).Status);
    }

    [StripeEurTestModeTheory]
    [InlineData(StripeApiVersion.V2025_04_30_Basil)]
    [InlineData(StripeApiVersion.V2026_09_30_Endive)]
    public async Task A_Multibanco_voucher_is_issued_with_its_entity_reference_and_page(StripeApiVersion version)
    {
        var response = await Stripe(version).Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Eur(12.90m),
            Method = PaymentMethodType.Multibanco,
            Customer = StripeIntents.Portuguese(),
        });

        Assert.True(response.Success, response.Error?.Message);
        Assert.Equal(PaymentStatus.Pending, response.Data!.Status);
        Assert.Equal(PaymentMethodType.Multibanco, response.Data.Method);
        var voucher = response.Data.Multibanco!;
        Assert.False(string.IsNullOrEmpty(voucher.Entity));
        Assert.False(string.IsNullOrEmpty(voucher.Reference));
        Assert.True(voucher.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.StartsWith("https://", voucher.HostedUrl);
    }

    [StripeEurTestModeFact]
    public async Task A_Multibanco_voucher_paid_at_once_is_paid_and_keeps_its_entity_and_reference()
    {
        var stripe = Stripe(StripeApiVersion.V2025_04_30_Basil);

        var created = await stripe.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Eur(12.90m),
            Method = PaymentMethodType.Multibanco,
            Customer = StripeIntents.Portuguese("succeed_immediately@example.com"),
        });

        Assert.True(created.Success, created.Error?.Message);
        var paid = await SettledAsync(stripe, created.Data!.Id);
        Assert.Equal(PaymentStatus.Paid, paid.Status);
        Assert.Equal(created.Data.Multibanco!.Entity, paid.Multibanco!.Entity);
        Assert.Equal(created.Data.Multibanco.Reference, paid.Multibanco.Reference);
    }
}
