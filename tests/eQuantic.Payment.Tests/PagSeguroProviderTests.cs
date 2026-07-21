using System.Net;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.PagSeguro;
using eQuantic.Payment.PagSeguro.Orders;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

public class PagSeguroProviderTests
{
    private const string PixOrderJson = """
    {
      "id": "ORDE_ABC123",
      "reference_id": "order-9",
      "created_at": "2026-07-21T10:00:00.000-03:00",
      "qr_codes": [
        {
          "id": "QRCO_XYZ",
          "text": "00020101021226830014br.gov.bcb.pix...6304ABCD",
          "amount": { "value": 15000, "currency": "BRL" },
          "expiration_date": "2026-07-21T11:00:00-03:00",
          "links": [
            { "rel": "QRCODE.PNG", "href": "https://api.pagseguro.com/qrcode/QRCO_XYZ/png", "media": "image/png", "type": "GET" }
          ]
        }
      ]
    }
    """;

    private const string CardOrderJson = """
    {
      "id": "ORDE_CARD",
      "reference_id": "order-42",
      "created_at": "2026-07-21T10:00:00.000-03:00",
      "charges": [
        {
          "id": "CHAR_123",
          "reference_id": "order-42",
          "status": "PAID",
          "amount": { "value": 15000, "currency": "BRL", "summary": { "total": 15000, "paid": 15000, "refunded": 0 } },
          "payment_response": { "code": 20000, "message": "SUCESSO", "reference": "032416400102" },
          "payment_method": {
            "type": "CREDIT_CARD", "installments": 3,
            "card": { "brand": "visa", "first_digits": "411111", "last_digits": "1111", "exp_month": "12", "exp_year": "2030" }
          }
        }
      ]
    }
    """;

    private static (PagSeguroOrdersProvider provider, StubHttpMessageHandler handler) CreateProvider()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(PagSeguroDefaults.BaseUrl) };
        return (new PagSeguroOrdersProvider(new PagSeguroOrdersClient(http), TestMapperFactory.Create()), handler);
    }

    [Fact]
    public async Task CreateAsync_pix_maps_qr_code_to_unified_charge()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.Created, PixOrderJson);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.Pix,
            ReferenceId = "order-9",
            Customer = new CustomerRequest { Name = "João Silva", Email = "joao@ex.com", Document = "12345678909" },
        });

        Assert.True(response.Success);
        Assert.Equal("pagseguro@orders", response.Provider.Key);

        var charge = response.Data!;
        Assert.Equal("ORDE_ABC123", charge.Id); // PIX tracked by order id
        Assert.Equal(PaymentStatus.Pending, charge.Status);
        Assert.Equal(PaymentMethodType.Pix, charge.Method);
        Assert.Equal(15000, charge.Amount.AmountInCents);
        Assert.StartsWith("00020101", charge.Pix!.QrCode);
        Assert.EndsWith("/png", charge.Pix.QrCodeImageUrl);
    }

    [Fact]
    public async Task CreateAsync_card_maps_charge_with_centavos_and_tolerant_code()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.Created, CardOrderJson);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.CreditCard,
            Card = new CardDetails { Token = "ENCRYPTED_CARD", Installments = 3 },
            Customer = new CustomerRequest { Name = "Ana", Email = "ana@ex.com", Document = "12345678909" },
        });

        Assert.True(response.Success);
        var charge = response.Data!;
        Assert.Equal("CHAR_123", charge.Id);
        Assert.Equal(PaymentStatus.Paid, charge.Status);
        Assert.Equal(15000, charge.Amount.AmountInCents); // centavos, sent as amount.value
        Assert.Equal("visa", charge.Card!.Brand);
        Assert.Equal("1111", charge.Card.Last4);
        Assert.Equal(3, charge.Card.Installments);
        Assert.Contains("\"value\":15000", handler.LastRequestBody);
    }

    [Fact]
    public async Task Customers_are_unsupported_and_return_a_clear_error()
    {
        var (provider, _) = CreateProvider();
        var response = await provider.Customers.CreateAsync(new CustomerRequest { Name = "Ana", Email = "ana@ex.com" });

        Assert.False(response.Success);
        Assert.Contains("no standalone customer resource", response.Error!.Message);
    }
}
