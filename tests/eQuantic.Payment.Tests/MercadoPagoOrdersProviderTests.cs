using System.Net;
using eQuantic.Payment.MercadoPago;
using eQuantic.Payment.MercadoPago.Customers;
using eQuantic.Payment.MercadoPago.Orders;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

public class MercadoPagoOrdersProviderTests
{
    private const string PixOrderJson = """
    {
      "id": "ORD01ABCDEF",
      "status": "action_required",
      "status_detail": "waiting_transfer",
      "total_amount": "150.00",
      "external_reference": "order-9",
      "currency": "BRL",
      "created_date": "2026-07-21T10:00:00.000-03:00",
      "transactions": {
        "payments": [
          {
            "id": "PAY01ABCDEF",
            "status": "action_required",
            "status_detail": "waiting_transfer",
            "amount": "150.00",
            "payment_method": {
              "id": "pix",
              "type": "bank_transfer",
              "qr_code": "00020126...br.gov.bcb.pix",
              "qr_code_base64": "iVBORw0KGgoAAAA",
              "ticket_url": "https://mp/o/1"
            }
          }
        ]
      }
    }
    """;

    private static (MercadoPagoOrdersProvider provider, StubHttpMessageHandler handler) CreateProvider()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(MercadoPagoDefaults.BaseUrl) };
        var provider = new MercadoPagoOrdersProvider(new MercadoPagoOrdersClient(http), new MercadoPagoCustomerClient(http), TestMapperFactory.Create());
        return (provider, handler);
    }

    [Fact]
    public async Task CreateAsync_pix_maps_order_to_unified_charge()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.Created, PixOrderJson);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.Pix,
            ReferenceId = "order-9",
            Customer = new CustomerRequest { Name = "João Silva", Email = "joao@ex.com", Document = "123.456.789-09" },
        });

        Assert.True(response.Success);
        Assert.Equal("mercadopago@orders", response.Provider.Key);

        var charge = response.Data!;
        // Unified charge id is the ORDER id, so subsequent operations act on the order.
        Assert.Equal("ORD01ABCDEF", charge.Id);
        Assert.Equal("order-9", charge.ReferenceId);
        Assert.Equal(PaymentStatus.Pending, charge.Status); // action_required -> Pending
        Assert.Equal(PaymentMethodType.Pix, charge.Method);
        Assert.Equal(15000, charge.Amount.AmountInCents);
        Assert.Equal("00020126...br.gov.bcb.pix", charge.Pix!.QrCode);
    }

    [Fact]
    public async Task CreateAsync_sends_string_amount_and_nested_payment_method()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.Created, PixOrderJson);

        await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.Pix,
            Customer = new CustomerRequest { Name = "Ana", Email = "ana@ex.com" },
        });

        Assert.EndsWith("v1/orders", handler.RequestPaths[0]);
        // Orders API amounts are STRINGS.
        Assert.Contains("\"total_amount\":\"150.00\"", handler.LastRequestBody);
        Assert.Contains("\"type\":\"bank_transfer\"", handler.LastRequestBody);
        Assert.True(handler.LastRequest!.Headers.Contains("X-Idempotency-Key"));
    }
}
