using System.Net;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Stripe;
using eQuantic.Payment.Stripe.V1;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

public class StripeV1ProviderTests
{
    private const string PixIntentJson = """
    {
      "id": "pi_123",
      "status": "requires_action",
      "amount": 2500,
      "currency": "brl",
      "payment_method_types": ["pix"],
      "created": 1770000000,
      "next_action": {
        "type": "pix_display_qr_code",
        "pix_display_qr_code": {
          "data": "00020126...pix",
          "image_url_png": "https://stripe/qr.png",
          "expires_at": 1770003600
        }
      },
      "metadata": { "reference_id": "order-9" }
    }
    """;

    private static (StripeProviderV1 provider, StubHttpMessageHandler handler) CreateProvider(
        StripeApiVersion version = StripeApiVersion.V2025_04_30_Basil)
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(StripeDefaults.BaseUrl) };
        return (new StripeProviderV1(new StripeClientV1(http), version, TestMapperFactory.Create()), handler);
    }

    [Fact]
    public async Task CreateAsync_pix_maps_to_unified_charge_with_dated_version()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, PixIntentJson);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(25.00m),
            Method = PaymentMethodType.Pix,
            ReferenceId = "order-9",
        });

        Assert.True(response.Success);
        Assert.Equal("stripe", response.Provider.Name);
        Assert.Equal("2025-04-30.basil", response.Provider.Version);
        Assert.Equal("stripe@2025-04-30.basil", response.Provider.Key);

        var charge = response.Data!;
        Assert.Equal("pi_123", charge.Id);
        Assert.Equal("order-9", charge.ReferenceId);
        Assert.Equal(PaymentStatus.Pending, charge.Status);
        Assert.Equal(2500, charge.Amount.AmountInCents);
        Assert.Equal("00020126...pix", charge.Pix!.QrCode);
        Assert.Equal("https://stripe/qr.png", charge.Pix.QrCodeImageUrl);
    }

    [Fact]
    public async Task CreateAsync_sends_form_urlencoded_to_payment_intents()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, PixIntentJson);

        await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(25.00m),
            Method = PaymentMethodType.Pix,
        });

        Assert.EndsWith("payment_intents", handler.RequestPaths[0]);
        Assert.Contains("amount=2500", handler.LastRequestBody);
        Assert.Contains("currency=brl", handler.LastRequestBody);
        Assert.Contains("payment_method_data%5Btype%5D=pix", handler.LastRequestBody);
    }

    [Fact]
    public void Version_is_carried_from_options()
    {
        var (provider, _) = CreateProvider(StripeApiVersion.V2024_06_20);
        Assert.Equal("2024-06-20", provider.Info.Version);
    }

    [Fact]
    public async Task CreateAsync_failure_parses_stripe_error_code()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.PaymentRequired, """
        { "error": { "type": "card_error", "code": "card_declined", "decline_code": "insufficient_funds", "message": "Your card has insufficient funds." } }
        """);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(25.00m),
            Method = PaymentMethodType.CreditCard,
            Card = new CardDetails { Token = "tok_visa" },
        });

        Assert.False(response.Success);
        Assert.Equal("insufficient_funds", response.Error!.Code);
        Assert.Equal(402, response.Error.HttpStatusCode);
        Assert.Contains("insufficient funds", response.Error.Message);
    }
}
