using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.DependencyInjection;

/// <summary>
/// Fluent builder used by provider packages (AddPagarme, AddStripe, ...) to register
/// their <see cref="IPaymentProvider"/> adapters as keyed services.
/// </summary>
public sealed class PaymentBuilder
{
    internal PaymentBuilder(IServiceCollection services, PaymentRegistry registry)
    {
        Services = services;
        Registry = registry;
    }

    public IServiceCollection Services { get; }

    public PaymentRegistry Registry { get; }

    /// <summary>
    /// Registers a provider adapter under <c>name@version</c>. Called by provider packages;
    /// call it directly only to plug in a custom provider.
    /// </summary>
    public PaymentBuilder AddProvider(
        ProviderInfo info,
        Func<IServiceProvider, IPaymentProvider> factory,
        bool asDefault = false)
    {
        Services.AddKeyedSingleton(NormalizeKey(info.Key), (sp, _) => factory(sp));
        Registry.Register(info, asDefault);
        return this;
    }

    internal static string NormalizeKey(string key) => key.ToLowerInvariant();
}
