using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.MercadoPago.Customers;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.MercadoPago.Orders;

/// <summary>Unified <see cref="IPaymentProvider"/> adapter over the Mercado Pago Orders API.</summary>
public sealed class MercadoPagoOrdersProvider : IPaymentProvider
{
    public MercadoPagoOrdersProvider(MercadoPagoOrdersClient client, MercadoPagoCustomerClient customerClient, IMapperFactory mappers)
    {
        Info = new ProviderInfo(MercadoPagoDefaults.ProviderName, MercadoPagoDefaults.VersionString(MercadoPagoApiVersion.Orders));
        var operations = new MercadoPagoOrdersOperations(client, Info, mappers);
        Charges = operations;
        Refunds = operations;
        Customers = new MercadoPagoCustomerOperations(customerClient, Info, mappers);
    }

    public ProviderInfo Info { get; }
    public IChargeOperations Charges { get; }
    public IRefundOperations Refunds { get; }
    public ICustomerOperations Customers { get; }
}
