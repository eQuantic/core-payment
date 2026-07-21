using eQuantic.Payment.Abstractions;
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Exceptions;
using eQuantic.Payment.MercadoPago;
using eQuantic.Payment.Pagarme;
using eQuantic.Payment.Stripe;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.Tests;

public class PaymentFactoryTests
{
    private static IPaymentProviderFactory BuildFactory(Action<PaymentBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddPayments(configure);
        return services.BuildServiceProvider().GetRequiredService<IPaymentProviderFactory>();
    }

    [Fact]
    public void Default_provider_is_the_one_flagged_as_default()
    {
        var factory = BuildFactory(p => p
            .AddPagarme(o => { o.ApiKey = "sk_test"; o.Version = PagarmeApiVersion.V5; })
            .AddStripe(o => { o.ApiKey = "sk_test"; }, asDefault: true));

        Assert.Equal("stripe", factory.GetDefault().Info.Name);
    }

    [Fact]
    public void Get_by_name_resolves_provider()
    {
        var factory = BuildFactory(p => p
            .AddPagarme(o => { o.ApiKey = "sk_test"; o.Version = PagarmeApiVersion.V5; }, asDefault: true)
            .AddStripe(o => { o.ApiKey = "sk_test"; }));

        Assert.Equal("pagarme@v5", factory.Get("pagarme").Info.Key);
        Assert.Equal("stripe", factory.Get("stripe").Info.Name);
    }

    [Fact]
    public void Same_provider_two_versions_coexist_and_resolve_by_version()
    {
        var factory = BuildFactory(p => p
            .AddStripe(o => { o.ApiKey = "sk_a"; o.Version = StripeApiVersion.V2024_06_20; })
            .AddStripe(o => { o.ApiKey = "sk_b"; o.Version = StripeApiVersion.V2025_04_30_Basil; }));

        Assert.Equal("2024-06-20", factory.Get("stripe", "2024-06-20").Info.Version);
        Assert.Equal("2025-04-30.basil", factory.Get("stripe", "2025-04-30.basil").Info.Version);

        // Get-by-name returns the latest registered version for that provider.
        Assert.Equal("2025-04-30.basil", factory.Get("stripe").Info.Version);
    }

    [Fact]
    public void GetRegistered_lists_every_provider_version_pair()
    {
        var factory = BuildFactory(p => p
            .AddPagarme(o => { o.ApiKey = "sk_test"; o.Version = PagarmeApiVersion.V4; })
            .AddPagarme(o => { o.ApiKey = "sk_test"; o.Version = PagarmeApiVersion.V5; })
            .AddStripe(o => { o.ApiKey = "sk_test"; }));

        var keys = factory.GetRegistered().Select(p => p.Key).ToArray();
        Assert.Contains("pagarme@v4", keys);
        Assert.Contains("pagarme@v5", keys);
        Assert.Contains("stripe@2025-04-30.basil", keys);
    }

    [Fact]
    public void Get_unregistered_provider_throws()
    {
        var factory = BuildFactory(p => p.AddStripe(o => { o.ApiKey = "sk_test"; }));
        Assert.Throws<ProviderNotRegisteredException>(() => factory.Get("cielo"));
    }

    [Fact]
    public void Mercadopago_both_api_versions_coexist_and_resolve_by_version()
    {
        var factory = BuildFactory(p => p
            .AddMercadoPago(o => { o.AccessToken = "TEST-1"; o.Version = MercadoPagoApiVersion.Payments; })
            .AddMercadoPago(o => { o.AccessToken = "TEST-2"; o.Version = MercadoPagoApiVersion.Orders; }));

        Assert.Equal("mercadopago@payments", factory.Get("mercadopago", "payments").Info.Key);
        Assert.Equal("mercadopago@orders", factory.Get("mercadopago", "orders").Info.Key);
        Assert.Equal("orders", factory.Get("mercadopago").Info.Version); // latest registered
    }
}
