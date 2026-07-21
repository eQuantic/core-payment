using System.Net;
using eQuantic.Payment.Cielo;
using eQuantic.Payment.Cielo.V3;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

public class CieloProviderTests
{
    private static (CieloV3Provider provider, StubHttpMessageHandler handler) CreateProvider()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(CieloDefaults.BaseUrl) };
        return (new CieloV3Provider(new CieloClient(http, CieloDefaults.QueryBaseUrl), TestMapperFactory.Create()), handler);
    }

    [Fact]
    public async Task CreateAsync_pix_maps_to_unified_charge_in_centavos()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.Created, """
        {
          "MerchantOrderId": "order-9",
          "Payment": {
            "PaymentId": "a1b2c3d4-0000",
            "Type": "Pix",
            "Amount": 15000,
            "Status": 12,
            "QrCodeString": "00020101021226830014br.gov.bcb.pix...6304ABCD",
            "QrCodeBase64Image": "iVBORw0KGgoAAAA"
          }
        }
        """);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.Pix,
            ReferenceId = "order-9",
        });

        Assert.True(response.Success);
        Assert.Equal("cielo@3.0", response.Provider.Key);

        var charge = response.Data!;
        Assert.Equal("a1b2c3d4-0000", charge.Id);
        Assert.Equal(PaymentStatus.Pending, charge.Status); // status 12
        Assert.Equal(15000, charge.Amount.AmountInCents);
        Assert.StartsWith("00020101", charge.Pix!.QrCode);
        Assert.StartsWith("data:image/png;base64,", charge.Pix.QrCodeImageUrl);
        Assert.Contains("\"Type\":\"Pix\"", handler.LastRequestBody);
        Assert.Contains("\"Amount\":15000", handler.LastRequestBody);
    }

    [Fact]
    public async Task CreateAsync_card_maps_status_and_brand()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.Created, """
        {
          "MerchantOrderId": "order-42",
          "Payment": {
            "PaymentId": "pid-1",
            "Type": "CreditCard",
            "Amount": 15000,
            "Status": 2,
            "AuthorizationCode": "123456",
            "Installments": 3,
            "CreditCard": { "Brand": "Visa", "CardNumber": "411111******1111" }
          }
        }
        """);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.CreditCard,
            Card = new CardDetails { Number = "4111111111111111", HolderName = "ANA", ExpirationMonth = 12, ExpirationYear = 2030, Cvv = "123", Brand = "Visa", Installments = 3 },
        });

        Assert.True(response.Success);
        var charge = response.Data!;
        Assert.Equal(PaymentStatus.Paid, charge.Status); // status 2 = PaymentConfirmed
        Assert.Equal("Visa", charge.Card!.Brand);
        Assert.Equal("1111", charge.Card.Last4);
        Assert.Equal(3, charge.Card.Installments);
    }
}
