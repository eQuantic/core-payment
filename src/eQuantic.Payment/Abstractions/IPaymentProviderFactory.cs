using eQuantic.Payment.Models;

namespace eQuantic.Payment.Abstractions;

/// <summary>Resolves registered payment providers by name and, optionally, API version.</summary>
public interface IPaymentProviderFactory
{
    /// <summary>Returns the default provider (the one registered with <c>asDefault: true</c>, or the first registered).</summary>
    IPaymentProvider GetDefault();

    /// <summary>Returns the provider registered under <paramref name="providerName"/> (latest registered version for that name).</summary>
    IPaymentProvider Get(string providerName);

    /// <summary>Returns the provider registered under <paramref name="providerName"/> pinned to <paramref name="version"/>.</summary>
    IPaymentProvider Get(string providerName, string version);

    /// <summary>All registered provider/version pairs.</summary>
    IReadOnlyCollection<ProviderInfo> GetRegistered();
}
