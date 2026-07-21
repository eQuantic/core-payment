using eQuantic.Mapper;
using eQuantic.Payment.Adyen.V71;
using eQuantic.Payment.Asaas.V3;
using eQuantic.Payment.Cielo.V3;
using eQuantic.Payment.Efi.Pix;
using eQuantic.Payment.MercadoPago.Payments;
using eQuantic.Payment.PagSeguro.Orders;
using eQuantic.Payment.Pagarme.V5;
using eQuantic.Payment.Stripe.V1;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.Tests.Fakes;

/// <summary>Builds an <see cref="IMapperFactory"/> with the provider mappers registered, for tests that construct providers directly.</summary>
public static class TestMapperFactory
{
    public static IMapperFactory Create()
    {
        var services = new ServiceCollection();
        services.AddMappers(o => o
            .FromAssembly(typeof(PagarmeProviderV5).Assembly)
            .FromAssembly(typeof(StripeProviderV1).Assembly)
            .FromAssembly(typeof(MercadoPagoPaymentsProvider).Assembly)
            .FromAssembly(typeof(PagSeguroOrdersProvider).Assembly)
            .FromAssembly(typeof(EfiPixProvider).Assembly)
            .FromAssembly(typeof(CieloV3Provider).Assembly)
            .FromAssembly(typeof(AdyenV71Provider).Assembly)
            .FromAssembly(typeof(AsaasV3Provider).Assembly));
        return services.BuildServiceProvider().GetRequiredService<IMapperFactory>();
    }
}
