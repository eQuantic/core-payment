using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Efi.Pix;

/// <summary>Unified <see cref="IPaymentProvider"/> adapter over the Efí Pix API (PIX only).</summary>
public sealed class EfiPixProvider : IPaymentProvider
{
    public EfiPixProvider(EfiPixClient client, IMapperFactory mappers, string? pixKey)
    {
        Info = new ProviderInfo(EfiDefaults.ProviderName, EfiDefaults.VersionString(EfiApiVersion.Pix));
        var operations = new EfiPixOperations(client, Info, mappers, pixKey);
        Charges = operations;
        Refunds = operations;
        Customers = new UnsupportedCustomerOperations(Info);
    }

    public ProviderInfo Info { get; }
    public IChargeOperations Charges { get; }
    public IRefundOperations Refunds { get; }
    public ICustomerOperations Customers { get; }
}
