using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Stripe.V1;

/// <summary>Unified <see cref="IPaymentProvider"/> adapter over <see cref="StripeClientV1"/>.</summary>
public sealed class StripeProviderV1 : IPaymentProvider
{
    public StripeProviderV1(StripeClientV1 client, StripeApiVersion version, IMapperFactory mappers)
    {
        Info = new ProviderInfo(StripeDefaults.ProviderName, StripeDefaults.VersionString(version));
        var operations = new StripeV1Operations(client, Info, mappers);
        Charges = operations;
        Refunds = operations;
        Customers = operations;
    }

    public ProviderInfo Info { get; }
    public IChargeOperations Charges { get; }
    public IRefundOperations Refunds { get; }
    public ICustomerOperations Customers { get; }
}
