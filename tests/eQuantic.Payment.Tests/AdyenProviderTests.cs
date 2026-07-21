using System.Net;
using eQuantic.Payment.Adyen;
using eQuantic.Payment.Adyen.V71;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

public class AdyenProviderTests
{
    private static (AdyenV71Provider provider, StubHttpMessageHandler handler) CreateProvider()
    {
        var handler = new StubHttpMessageHandler();
        var http = new HttpClient(handler) { BaseAddress = new Uri(AdyenDefaults.BaseUrl) };
        return (new AdyenV71Provider(new AdyenClientV71(http), "TestMerchant", AdyenApiVersion.V71, TestMapperFactory.Create()), handler);
    }

    [Fact]
    public async Task CreateAsync_pix_maps_pending_action_to_unified_charge()
    {
        var (provider, handler) = CreateProvider();
        handler.EnqueueJson(HttpStatusCode.OK, """
        {
          "resultCode": "Pending",
          "pspReference": "PSP123",
          "action": { "type": "qrCode", "qrCodeData": "00020101...br.gov.bcb.pix", "paymentMethodType": "pix" },
          "additionalData": { "pix.expirationDate": "2026-07-21T11:00:00Z" }
        }
        """);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.Pix,
            ReferenceId = "order-9",
        });

        Assert.True(response.Success);
        Assert.Equal("adyen@v71", response.Provider.Key);

        var charge = response.Data!;
        Assert.Equal("PSP123", charge.Id);
        Assert.Equal(PaymentStatus.Pending, charge.Status);
        Assert.Equal(PaymentMethodType.Pix, charge.Method);
        Assert.Equal(15000, charge.Amount.AmountInCents); // resolved from the request via the context mapper
        Assert.StartsWith("00020101", charge.Pix!.QrCode);
        // merchantAccount is stamped onto the request body.
        Assert.Contains("TestMerchant", handler.LastRequestBody);
        Assert.Contains("\"value\":15000", handler.LastRequestBody);
    }

    [Fact]
    public async Task CreateAsync_refusal_is_a_failed_response_not_success()
    {
        var (provider, handler) = CreateProvider();
        // A refusal is HTTP 200 with resultCode "Refused" — must NOT be treated as success.
        handler.EnqueueJson(HttpStatusCode.OK, """
        { "resultCode": "Refused", "pspReference": "PSP456", "refusalReason": "Not enough balance", "refusalReasonCode": "1" }
        """);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Brl(150.00m),
            Method = PaymentMethodType.CreditCard,
            Card = new CardDetails { Token = "stored_pm_1" },
        });

        Assert.False(response.Success);
        Assert.Equal("adyen@v71", response.Provider.Key);
        Assert.Contains("Not enough balance", response.Error!.Message);
    }
}
