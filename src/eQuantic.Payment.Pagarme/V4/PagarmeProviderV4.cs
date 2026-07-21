using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Pagarme.V4;

/// <summary>Unified <see cref="IPaymentProvider"/> adapter over <see cref="PagarmeClientV4"/> (legacy transaction API).</summary>
public sealed class PagarmeProviderV4 : IPaymentProvider
{
    public PagarmeProviderV4(PagarmeClientV4 client, IMapperFactory mappers)
    {
        Info = new ProviderInfo(PagarmeDefaults.ProviderName, PagarmeDefaults.VersionString(PagarmeApiVersion.V4));
        var operations = new PagarmeV4Operations(client, Info, mappers);
        Charges = operations;
        Refunds = operations;
        Customers = operations;
    }

    public ProviderInfo Info { get; }
    public IChargeOperations Charges { get; }
    public IRefundOperations Refunds { get; }
    public ICustomerOperations Customers { get; }
}
