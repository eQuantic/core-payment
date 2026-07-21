using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.MercadoPago.Customers;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.MercadoPago.Payments;

/// <summary>Unified <see cref="IPaymentProvider"/> adapter over the Mercado Pago Payments API.</summary>
public sealed class MercadoPagoPaymentsProvider : IPaymentProvider
{
    public MercadoPagoPaymentsProvider(MercadoPagoPaymentsClient client, MercadoPagoCustomerClient customerClient, IMapperFactory mappers)
    {
        Info = new ProviderInfo(MercadoPagoDefaults.ProviderName, MercadoPagoDefaults.VersionString(MercadoPagoApiVersion.Payments));
        var operations = new MercadoPagoPaymentsOperations(client, Info, mappers);
        Charges = operations;
        Refunds = operations;
        Customers = new MercadoPagoCustomerOperations(customerClient, Info, mappers);
    }

    public ProviderInfo Info { get; }
    public IChargeOperations Charges { get; }
    public IRefundOperations Refunds { get; }
    public ICustomerOperations Customers { get; }
}
