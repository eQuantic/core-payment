using System.Net;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Pagarme;
using eQuantic.Payment.Pagarme.V5;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

public class PagarmeV5ProviderTests
{
    private const string PixOrderJson = """
    {
      "id": "or_123",
      "code": "order-ref-1",
      "status": "pending",
      "charges": [
        {
          "id": "ch_abc",
          "status": "pending",
          "amount": 1500,
          "payment_method": "pix",
          "created_at": "2026-07-21T10:00:00Z",
          "last_transaction": {
            "transaction_type": "pix",
            "qr_code": "00020126...br.gov.bcb.pix",
            "qr_code_url": "https://pagarme/qr.png",
            "expires_at": "2026-07-21T11:00:00Z"
          }
        }
      ]
    }
    """;

    private static (PagarmeProviderV5 provider, StubHttpMessageHandler handler) CreateProvider()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(PagarmeDefaults.V5BaseUrl) };
        return (new PagarmeProviderV5(new PagarmeClientV5(http), TestMapperFactory.Create()), handler);
    }

    [Fact]
    public async Task CreateAsync_pix_maps_to_unified_charge_with_provider_and_version()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, PixOrderJson);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(15.00m),
            Method = PaymentMethodType.Pix,
            ReferenceId = "order-ref-1",
            Customer = new CustomerRequest { Name = "João", Email = "joao@ex.com", Document = "12345678909" },
        });

        Assert.True(response.Success);
        Assert.Equal("pagarme", response.Provider.Name);
        Assert.Equal("v5", response.Provider.Version);
        Assert.Equal("pagarme@v5", response.Provider.Key);

        var charge = response.Data!;
        Assert.Equal("ch_abc", charge.Id);
        Assert.Equal("order-ref-1", charge.ReferenceId);
        Assert.Equal(PaymentStatus.Pending, charge.Status);
        Assert.Equal("pending", charge.ProviderStatus);
        Assert.Equal(1500, charge.Amount.AmountInCents);
        Assert.Equal(PaymentMethodType.Pix, charge.Method);
        Assert.Equal("00020126...br.gov.bcb.pix", charge.Pix!.QrCode);
        Assert.Equal("https://pagarme/qr.png", charge.Pix.QrCodeImageUrl);
    }

    [Fact]
    public async Task CreateAsync_posts_to_orders_endpoint_with_snake_case_body()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, PixOrderJson);

        await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(15.00m),
            Method = PaymentMethodType.Pix,
        });

        Assert.EndsWith("orders", handler.RequestPaths[0]);
        Assert.Contains("payment_method", handler.LastRequestBody);
        Assert.Contains("\"pix\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task CreateAsync_auth_only_maps_authorized_status_from_last_transaction()
    {
        // The charge-level status stays "pending" for an auth-only card charge; the "authorized"
        // signal lives in last_transaction.status and must win.
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """
        {
          "id": "or_a", "status": "pending",
          "charges": [{
            "id": "ch_a", "status": "pending", "amount": 5000, "payment_method": "credit_card",
            "last_transaction": {
              "transaction_type": "credit_card", "status": "authorized_pending_capture", "installments": 1,
              "acquirer_auth_code": "123456",
              "card": { "brand": "Visa", "last_four_digits": "1234" }
            }
          }]
        }
        """);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(50m),
            Method = PaymentMethodType.CreditCard,
            Capture = false,
            Card = new CardDetails { Token = "card_xyz" },
        });

        Assert.True(response.Success);
        var charge = response.Data!;
        Assert.Equal(PaymentStatus.Authorized, charge.Status);
        Assert.Equal("authorized_pending_capture", charge.ProviderStatus);
        Assert.Equal("Visa", charge.Card!.Brand);
        Assert.Equal("1234", charge.Card.Last4);
        Assert.Equal("123456", charge.Card.AuthorizationCode);
    }

    [Fact]
    public async Task CreateAsync_failure_returns_error_envelope_with_raw_body()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.BadRequest, """{"message":"invalid amount"}""");

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.FromCents(0),
            Method = PaymentMethodType.Pix,
        });

        Assert.False(response.Success);
        Assert.Equal("pagarme@v5", response.Provider.Key);
        Assert.Equal(400, response.Error!.HttpStatusCode);
        Assert.Contains("invalid amount", response.RawResponse);
    }
}
