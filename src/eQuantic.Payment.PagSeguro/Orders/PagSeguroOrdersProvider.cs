using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.PagSeguro.Orders;

/// <summary>Unified <see cref="IPaymentProvider"/> adapter over the PagSeguro Orders/Charges API.</summary>
public sealed class PagSeguroOrdersProvider : IPaymentProvider
{
    public PagSeguroOrdersProvider(PagSeguroOrdersClient client, IMapperFactory mappers)
    {
        Info = new ProviderInfo(PagSeguroDefaults.ProviderName, PagSeguroDefaults.VersionString(PagSeguroApiVersion.Orders));
        var operations = new PagSeguroOrdersOperations(client, Info, mappers);
        Charges = operations;
        Refunds = operations;
        // The Orders API has no standalone customer resource; customer data is sent inline with the order.
        Customers = new UnsupportedCustomerOperations(Info);
    }

    public ProviderInfo Info { get; }
    public IChargeOperations Charges { get; }
    public IRefundOperations Refunds { get; }
    public ICustomerOperations Customers { get; }
}
