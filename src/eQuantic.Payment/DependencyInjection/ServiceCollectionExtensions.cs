using eQuantic.Payment.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace eQuantic.Payment.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the unified payment infrastructure. Provider packages plug into the builder:
    /// <code>
    /// services.AddPayments(payments => payments
    ///     .AddPagarme(o => { o.ApiKey = "sk_..."; o.Version = PagarmeApiVersion.V5; }, asDefault: true)
    ///     .AddStripe(o => { o.ApiKey = "sk_..."; }));
    /// </code>
    /// </summary>
    public static IServiceCollection AddPayments(this IServiceCollection services, Action<PaymentBuilder> configure)
    {
        var registry = GetOrCreateRegistry(services);
        var builder = new PaymentBuilder(services, registry);
        configure(builder);

        services.TryAddSingleton<IPaymentProviderFactory, PaymentProviderFactory>();
        return services;
    }

    private static PaymentRegistry GetOrCreateRegistry(IServiceCollection services)
    {
        var existing = services
            .FirstOrDefault(d => d.ServiceType == typeof(PaymentRegistry))?
            .ImplementationInstance as PaymentRegistry;

        if (existing is not null)
        {
            return existing;
        }

        var registry = new PaymentRegistry();
        services.AddSingleton(registry);
        return registry;
    }
}
