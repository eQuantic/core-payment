using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Adyen;
using eQuantic.Payment.Adyen.V71;
using eQuantic.Payment.Asaas;
using eQuantic.Payment.Asaas.V3;
using eQuantic.Payment.Cielo;
using eQuantic.Payment.Cielo.V3;
using eQuantic.Payment.Efi;
using eQuantic.Payment.Efi.Cobrancas;
using eQuantic.Payment.Efi.Pix;
using eQuantic.Payment.MercadoPago;
using eQuantic.Payment.MercadoPago.Customers;
using eQuantic.Payment.MercadoPago.Orders;
using eQuantic.Payment.MercadoPago.Payments;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.PagSeguro;
using eQuantic.Payment.PagSeguro.Orders;
using eQuantic.Payment.Pagarme;
using eQuantic.Payment.Pagarme.V4;
using eQuantic.Payment.Pagarme.V5;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

/// <summary>
/// MB WAY and Multibanco are Stripe's here. Every other provider answers a charge in either with a failed response
/// and sends nothing, where its mapper would otherwise throw on a method it has no wire value for.
/// </summary>
public class UnsupportedPaymentMethodTypeTests
{
    public static TheoryData<string, PaymentMethodType> EveryOtherProviderAndMethod()
    {
        var data = new TheoryData<string, PaymentMethodType>();
        foreach (var provider in Providers.Keys)
        {
            data.Add(provider, PaymentMethodType.MbWay);
            data.Add(provider, PaymentMethodType.Multibanco);
        }

        return data;
    }

    private static readonly Dictionary<string, Func<StubHttpMessageHandler, IPaymentProvider>> Providers = new()
    {
        ["pagarme@v4"] = handler => new PagarmeProviderV4(new PagarmeClientV4(Http(handler, PagarmeDefaults.V4BaseUrl), "ak_test"), TestMapperFactory.Create()),
        ["pagarme@v5"] = handler => new PagarmeProviderV5(new PagarmeClientV5(Http(handler, PagarmeDefaults.V5BaseUrl)), TestMapperFactory.Create()),
        ["mercadopago@payments"] = handler =>
        {
            var http = Http(handler, MercadoPagoDefaults.BaseUrl);
            return new MercadoPagoPaymentsProvider(new MercadoPagoPaymentsClient(http), new MercadoPagoCustomerClient(http), TestMapperFactory.Create());
        },
        ["mercadopago@orders"] = handler =>
        {
            var http = Http(handler, MercadoPagoDefaults.BaseUrl);
            return new MercadoPagoOrdersProvider(new MercadoPagoOrdersClient(http), new MercadoPagoCustomerClient(http), TestMapperFactory.Create());
        },
        ["pagseguro@orders"] = handler => new PagSeguroOrdersProvider(new PagSeguroOrdersClient(Http(handler, PagSeguroDefaults.BaseUrl)), TestMapperFactory.Create()),
        ["cielo@3.0"] = handler => new CieloV3Provider(new CieloClient(Http(handler, CieloDefaults.BaseUrl), CieloDefaults.QueryBaseUrl), TestMapperFactory.Create()),
        ["adyen@v71"] = handler => new AdyenV71Provider(new AdyenClientV71(Http(handler, AdyenDefaults.BaseUrl)), "TestMerchant", AdyenApiVersion.V71, TestMapperFactory.Create()),
        ["asaas@v3"] = handler => new AsaasV3Provider(new AsaasV3Client(Http(handler, AsaasDefaults.BaseUrl)), TestMapperFactory.Create()),
        ["efi@pix"] = handler =>
        {
            var http = Http(handler, EfiDefaults.PixBaseUrl);
            return new EfiPixProvider(new EfiPixClient(http, new EfiTokenProvider(http, "id", "secret", EfiDefaults.PixTokenPath)), TestMapperFactory.Create(), "chave");
        },
        ["efi@cobrancas"] = handler =>
        {
            var http = Http(handler, EfiDefaults.CobrancasBaseUrl);
            return new EfiCobrancasProvider(new EfiCobrancasClient(http, new EfiTokenProvider(http, "id", "secret", EfiDefaults.CobrancasTokenPath)), TestMapperFactory.Create());
        },
    };

    private static HttpClient Http(StubHttpMessageHandler handler, string baseUrl) => new(handler) { BaseAddress = new Uri(baseUrl) };

    /// <summary>The values a consumer may have stored: adding methods must not move the ones that were there.</summary>
    [Fact]
    public void The_methods_that_existed_keep_their_values_and_the_new_ones_come_after()
    {
        Assert.Equal(0, (int)PaymentMethodType.CreditCard);
        Assert.Equal(1, (int)PaymentMethodType.DebitCard);
        Assert.Equal(2, (int)PaymentMethodType.Pix);
        Assert.Equal(3, (int)PaymentMethodType.Boleto);
        Assert.Equal(4, (int)PaymentMethodType.MbWay);
        Assert.Equal(5, (int)PaymentMethodType.Multibanco);
    }

    [Theory]
    [MemberData(nameof(EveryOtherProviderAndMethod))]
    public async Task Every_other_provider_refuses_MB_WAY_and_Multibanco_without_a_call(string key, PaymentMethodType method)
    {
        var handler = new StubHttpMessageHandler();
        var provider = Providers[key](handler);

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount = Money.Eur(25.00m),
            Method = method,
            Customer = StripeIntents.Portuguese(),
            MbWay = new MbWayDetails { PhoneNumber = "+351912345678" },
        });

        Assert.Equal(key, provider.Info.Key);
        Assert.False(response.Success);
        Assert.Equal(UnsupportedPaymentMethodType.Code, response.Error!.Code);
        Assert.Contains(key, response.Error.Message);
        Assert.Contains(method.ToString(), response.Error.Message);
        Assert.Empty(handler.Requests);
    }
}
