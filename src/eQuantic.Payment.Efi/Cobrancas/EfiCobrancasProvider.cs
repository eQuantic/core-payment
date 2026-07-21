using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Efi.Cobrancas;

/// <summary>Unified <see cref="IPaymentProvider"/> adapter over the Efí Cobranças API (boleto + credit card).</summary>
public sealed class EfiCobrancasProvider : IPaymentProvider
{
    public EfiCobrancasProvider(EfiCobrancasClient client, IMapperFactory mappers)
    {
        Info = new ProviderInfo(EfiDefaults.ProviderName, EfiDefaults.VersionString(EfiApiVersion.Cobrancas));
        var operations = new EfiCobrancasOperations(client, Info, mappers);
        Charges = operations;
        Refunds = operations;
        Customers = new UnsupportedCustomerOperations(Info);
    }

    public ProviderInfo Info { get; }
    public IChargeOperations Charges { get; }
    public IRefundOperations Refunds { get; }
    public ICustomerOperations Customers { get; }
}
