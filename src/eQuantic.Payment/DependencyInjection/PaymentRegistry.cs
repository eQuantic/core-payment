using eQuantic.Payment.Models;

namespace eQuantic.Payment.DependencyInjection;

/// <summary>Tracks which provider/version pairs were registered and which one is the default.</summary>
public sealed class PaymentRegistry
{
    private readonly List<ProviderInfo> _providers = [];

    public string? DefaultKey { get; private set; }

    public IReadOnlyCollection<ProviderInfo> Providers => _providers;

    internal void Register(ProviderInfo info, bool asDefault)
    {
        _providers.RemoveAll(p => p.Key == info.Key);
        _providers.Add(info);

        if (asDefault || DefaultKey is null)
        {
            DefaultKey = info.Key;
        }
    }

    /// <summary>Latest registered version for a provider name, or <c>null</c>.</summary>
    internal ProviderInfo? FindByName(string name)
        => _providers.LastOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    internal ProviderInfo? FindByKey(string name, string version)
        => _providers.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(p.Version, version, StringComparison.OrdinalIgnoreCase));
}
