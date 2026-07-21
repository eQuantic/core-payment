using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Exceptions;
using eQuantic.Payment.Models;
using Microsoft.Extensions.DependencyInjection;

namespace eQuantic.Payment.DependencyInjection;

internal sealed class PaymentProviderFactory(IServiceProvider serviceProvider, PaymentRegistry registry)
    : IPaymentProviderFactory
{
    public IPaymentProvider GetDefault()
    {
        var key = registry.DefaultKey
            ?? throw new PaymentException("No payment provider registered. Call AddPayments(...) and add at least one provider.");
        return Resolve(key);
    }

    public IPaymentProvider Get(string providerName)
    {
        var info = registry.FindByName(providerName)
            ?? throw new ProviderNotRegisteredException(providerName);
        return Resolve(info.Key);
    }

    public IPaymentProvider Get(string providerName, string version)
    {
        var info = registry.FindByKey(providerName, version)
            ?? throw new ProviderNotRegisteredException($"{providerName}@{version}");
        return Resolve(info.Key);
    }

    public IReadOnlyCollection<ProviderInfo> GetRegistered() => registry.Providers;

    private IPaymentProvider Resolve(string key)
        => serviceProvider.GetRequiredKeyedService<IPaymentProvider>(PaymentBuilder.NormalizeKey(key));
}
