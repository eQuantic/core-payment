using eQuantic.Mapper;
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
            .FromAssembly(typeof(StripeProviderV1).Assembly));
        return services.BuildServiceProvider().GetRequiredService<IMapperFactory>();
    }
}
