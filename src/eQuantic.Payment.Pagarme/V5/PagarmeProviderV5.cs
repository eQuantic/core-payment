using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Pagarme.V5;

/// <summary>Unified <see cref="IPaymentProvider"/> adapter over <see cref="PagarmeClientV5"/>.</summary>
public sealed class PagarmeProviderV5 : IPaymentProvider
{
    public PagarmeProviderV5(PagarmeClientV5 client, IMapperFactory mappers)
    {
        Info = new ProviderInfo(PagarmeDefaults.ProviderName, PagarmeDefaults.VersionString(PagarmeApiVersion.V5));
        Charges = new PagarmeV5ChargeOperations(client, Info, mappers);
        Refunds = new PagarmeV5RefundOperations(client, Info, mappers);
        Customers = new PagarmeV5CustomerOperations(client, Info, mappers);
    }

    public ProviderInfo Info { get; }
    public IChargeOperations Charges { get; }
    public IRefundOperations Refunds { get; }
    public ICustomerOperations Customers { get; }
}
