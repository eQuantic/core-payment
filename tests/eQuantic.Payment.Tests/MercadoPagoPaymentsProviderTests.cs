using System.Net;
using eQuantic.Payment.MercadoPago;
using eQuantic.Payment.MercadoPago.Customers;
using eQuantic.Payment.MercadoPago.Payments;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

public class MercadoPagoPaymentsProviderTests
{
    private const string PixPaymentJson = """
    {
      "id": 123456789,
      "status": "pending",
      "status_detail": "pending_waiting_transfer",
      "date_created": "2026-07-21T10:00:00.000-03:00",
      "payment_method_id": "pix",
      "payment_type_id": "bank_transfer",
      "currency_id": "BRL",
      "transaction_amount": 150.00,
      "external_reference": "order-42",
      "date_of_expiration": "2026-07-21T11:00:00.000-03:00",
      "point_of_interaction": {
        "type": "OPENPLATFORM",
        "transaction_data": {
          "qr_code": "00020126...br.gov.bcb.pix",
          "qr_code_base64": "iVBORw0KGgoAAAA",
          "ticket_url": "https://mp/pix/123"
        }
      }
    }
    """;

    private static (MercadoPagoPaymentsProvider provider, StubHttpMessageHandler handler) CreateProvider()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(MercadoPagoDefaults.BaseUrl) };
        var provider = new MercadoPagoPaymentsProvider(new MercadoPagoPaymentsClient(http), new MercadoPagoCustomerClient(http), TestMapperFactory.Create());
        return (provider, handler);
    }

    [Fact]
    public async Task CreateAsync_pix_maps_to_unified_charge_with_provider_and_version()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.Created, PixPaymentJson);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.Pix,
            ReferenceId = "order-42",
            Customer = new CustomerRequest { Name = "João Silva", Email = "joao@ex.com", Document = "123.456.789-09" },
        });

        Assert.True(response.Success);
        Assert.Equal("mercadopago", response.Provider.Name);
        Assert.Equal("payments", response.Provider.Version);
        Assert.Equal("mercadopago@payments", response.Provider.Key);

        var charge = response.Data!;
        Assert.Equal("123456789", charge.Id);
        Assert.Equal("order-42", charge.ReferenceId);
        Assert.Equal(PaymentStatus.Pending, charge.Status);
        Assert.Equal(PaymentMethodType.Pix, charge.Method);
        // 150.00 reais round-trips to 15000 centavos in the unified Money.
        Assert.Equal(15000, charge.Amount.AmountInCents);
        Assert.Equal("00020126...br.gov.bcb.pix", charge.Pix!.QrCode);
        Assert.Equal("data:image/png;base64,iVBORw0KGgoAAAA", charge.Pix.QrCodeImageUrl);
    }

    [Fact]
    public async Task CreateAsync_sends_decimal_amount_and_idempotency_key()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.Created, PixPaymentJson);

        await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.Pix,
            Customer = new CustomerRequest { Name = "Ana", Email = "ana@ex.com" },
        });

        Assert.EndsWith("v1/payments", handler.RequestPaths[0]);
        Assert.Contains("transaction_amount", handler.LastRequestBody);
        Assert.Contains("\"payment_method_id\":\"pix\"", handler.LastRequestBody);
        // Amount is decimal reais (150), NOT centavos (15000).
        Assert.DoesNotContain("15000", handler.LastRequestBody);
        Assert.True(handler.LastRequest!.Headers.Contains("X-Idempotency-Key"));
    }

    [Fact]
    public async Task CreateAsync_failure_parses_mercadopago_error_cause()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.BadRequest, """
        { "message": "invalid transaction_amount", "error": "bad_request", "status": 400,
          "cause": [ { "code": 2001, "description": "transaction_amount must be greater than 0" } ] }
        """);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.FromCents(0),
            Method = PaymentMethodType.Pix,
            Customer = new CustomerRequest { Name = "Ana", Email = "ana@ex.com" },
        });

        Assert.False(response.Success);
        Assert.Equal("mercadopago@payments", response.Provider.Key);
        Assert.Equal(400, response.Error!.HttpStatusCode);
        Assert.Equal("2001", response.Error.Code);
        Assert.Contains("greater than 0", response.Error.Message);
    }
}
