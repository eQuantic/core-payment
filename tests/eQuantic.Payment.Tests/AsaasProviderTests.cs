using System.Net;
using eQuantic.Payment.Asaas;
using eQuantic.Payment.Asaas.V3;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

public class AsaasProviderTests
{
    private static (AsaasV3Provider provider, StubHttpMessageHandler handler) CreateProvider()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(AsaasDefaults.BaseUrl) };
        return (new AsaasV3Provider(new AsaasV3Client(http), TestMapperFactory.Create()), handler);
    }

    [Fact]
    public async Task CreateAsync_pix_creates_customer_then_payment_then_fetches_qr()
    {
        var (provider, handler) = CreateProvider();
        // Sequence: POST /customers → POST /payments → GET /payments/{id}/pixQrCode
        handler.EnqueueJson(HttpStatusCode.OK, """{ "id": "cus_1", "name": "João", "email": "joao@ex.com" }""")
               .EnqueueJson(HttpStatusCode.OK, """
               { "id": "pay_1", "status": "PENDING", "billingType": "PIX", "value": 150.00, "externalReference": "order-9", "dateCreated": "2026-07-21" }
               """)
               .EnqueueJson(HttpStatusCode.OK, """
               { "encodedImage": "iVBORw0KGgoAAAA", "payload": "00020101021226830014br.gov.bcb.pix...6304ABCD", "expirationDate": "2026-07-21 11:00:00" }
               """);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.Pix,
            ReferenceId = "order-9",
            Customer = new CustomerRequest { Name = "João", Email = "joao@ex.com", Document = "12345678909" },
        });

        Assert.True(response.Success);
        Assert.Equal("asaas@v3", response.Provider.Key);

        var charge = response.Data!;
        Assert.Equal("pay_1", charge.Id);
        Assert.Equal(PaymentStatus.Pending, charge.Status);
        Assert.Equal(PaymentMethodType.Pix, charge.Method);
        Assert.Equal(15000, charge.Amount.AmountInCents); // 150.00 reais decimal → 15000 centavos in unified Money
        Assert.StartsWith("00020101", charge.Pix!.QrCode);
        Assert.StartsWith("data:image/png;base64,", charge.Pix.QrCodeImageUrl);

        // Three calls were made in order: customer, payment, pix QR.
        Assert.Equal(3, handler.RequestPaths.Count);
        Assert.EndsWith("customers", handler.RequestPaths[0]);
        Assert.EndsWith("payments", handler.RequestPaths[1]);
        Assert.EndsWith("pixQrCode", handler.RequestPaths[2]);
    }

    [Fact]
    public async Task Customers_are_supported()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """{ "id": "cus_9", "name": "Ana", "email": "ana@ex.com", "cpfCnpj": "12345678909" }""");

        var response = await provider.Customers.CreateAsync(new CustomerRequest { Name = "Ana", Email = "ana@ex.com", Document = "12345678909" });

        Assert.True(response.Success);
        Assert.Equal("cus_9", response.Data!.Id);
    }
}
