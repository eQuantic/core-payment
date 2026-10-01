using System.Net;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Stripe;
using eQuantic.Payment.Stripe.V1;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

/// <summary>
/// Regression tests for the modern Stripe schema: card details come from the expanded
/// <c>latest_charge</c> (the old <c>charges[]</c> list was removed in API 2022-11-15).
/// </summary>
public class StripeLatestChargeTests
{
    private static (StripeProviderV1 provider, StubHttpMessageHandler handler) CreateProvider(StubHttpMessageHandler handler, DateTimeOffset now)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri(StripeDefaults.BaseUrl) };
        var provider = new StripeProviderV1(
            new StripeClientV1(http), StripeApiVersion.V2025_04_30_Basil, TestMapperFactory.Create(), null, StripeDefaults.WebhookTolerance, new FixedClock(now));
        return (provider, handler);
    }

    private static (StripeProviderV1 provider, StubHttpMessageHandler handler) CreateProvider()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(StripeDefaults.BaseUrl) };
        return (new StripeProviderV1(new StripeClientV1(http), StripeApiVersion.V2025_04_30_Basil, TestMapperFactory.Create()), handler);
    }

    [Fact]
    public async Task Card_details_are_read_from_expanded_latest_charge()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """
        {
          "id": "pi_card", "status": "succeeded", "amount": 5000, "currency": "brl",
          "payment_method_types": ["card"],
          "latest_charge": {
            "id": "ch_1", "status": "succeeded",
            "payment_method_details": {
              "type": "card",
              "card": {
                "brand": "visa", "last4": "4242", "authorization_code": "AB1234",
                "installments": { "plan": { "count": 3, "interval": "month", "type": "fixed_count" } }
              }
            }
          }
        }
        """);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(50m),
            Method = PaymentMethodType.CreditCard,
            Card = new CardDetails { Token = "pm_card_visa", Installments = 3 },
        });

        Assert.True(response.Success);
        var card = response.Data!.Card!;
        Assert.Equal("visa", card.Brand);
        Assert.Equal("4242", card.Last4);
        Assert.Equal("AB1234", card.AuthorizationCode);
        Assert.Equal(3, card.Installments);
    }

    [Fact]
    public async Task Bare_string_latest_charge_deserializes_without_error()
    {
        var (provider, handler) = CreateProvider();
        // When not expanded, latest_charge is a bare id string — must not break deserialization.
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "id": "pi_x", "status": "processing", "amount": 1000, "currency": "brl",
          "payment_method_types": ["card"], "latest_charge": "ch_bare" }
        """);

        var response = await provider.Charges.GetAsync("pi_x");

        Assert.True(response.Success);
        Assert.Equal("pi_x", response.Data!.Id);
        Assert.Equal(1, response.Data.Card!.Installments); // defaults when charge not expanded
    }

    [Fact]
    public async Task Get_request_expands_latest_charge_in_query()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """{ "id": "pi_x", "status": "succeeded", "amount": 1000, "currency": "brl", "payment_method_types": ["pix"] }""");

        await provider.Charges.GetAsync("pi_x");

        Assert.Contains("expand", handler.RequestPaths[0]);
        Assert.Contains("latest_charge", handler.RequestPaths[0]);
    }

    [Fact]
    public async Task Boleto_uses_expires_after_days_and_full_billing_address()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "id": "pi_bol", "status": "requires_action", "amount": 9900, "currency": "brl",
          "payment_method_types": ["boleto"],
          "next_action": { "type": "boleto_display_details",
            "boleto_display_details": { "number": "34191.79001", "pdf": "https://stripe/boleto.pdf", "expires_at": 1893456000 } } }
        """);

        // 22:30 in São Paulo is already the next day in UTC: the voucher's days count from São Paulo's today.
        var now = new DateTimeOffset(2026, 9, 30, 22, 30, 0, TimeSpan.FromHours(-3));
        (provider, handler) = CreateProvider(handler, now);
        var today = new DateOnly(2026, 9, 30);
        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(99m),
            Method = PaymentMethodType.Boleto,
            Customer = new CustomerRequest
            {
                Name = "Maria", Email = "maria@ex.com", Document = "123.456.789-09",
                Address = new AddressRequest
                {
                    Line1 = "Av. Paulista, 1000", City = "São Paulo", State = "SP", ZipCode = "01310-000",
                },
            },
            Boleto = new BoletoDetails { DueDate = today.AddDays(5) },
        });

        Assert.True(response.Success);
        var body = handler.LastRequestBody!;
        // Days-based expiry, NOT an absolute timestamp.
        Assert.Contains("payment_method_options%5Bboleto%5D%5Bexpires_after_days%5D=5", body);
        Assert.DoesNotContain("expires_at", body);
        // tax_id + full billing address are required by Stripe for boleto.
        Assert.Contains("payment_method_data%5Bboleto%5D%5Btax_id%5D=12345678909", body);
        Assert.Contains("payment_method_data%5Bbilling_details%5D%5Baddress%5D%5Bline1%5D=", body);
        Assert.Contains("payment_method_data%5Bbilling_details%5D%5Baddress%5D%5Bstate%5D=SP", body);
    }
}
