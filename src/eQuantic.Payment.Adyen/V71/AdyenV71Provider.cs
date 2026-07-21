using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Adyen.V71;

/// <summary>Unified <see cref="IPaymentProvider"/> adapter over the Adyen Checkout API v71.</summary>
public sealed class AdyenV71Provider : IPaymentProvider
{
    public AdyenV71Provider(AdyenClientV71 client, string merchantAccount, AdyenApiVersion version, IMapperFactory mappers)
    {
        Info = new ProviderInfo(AdyenDefaults.ProviderName, AdyenDefaults.VersionString(version));
        var operations = new AdyenV71Operations(client, Info, merchantAccount, mappers);
        Charges = operations;
        Refunds = operations;
        // Adyen sends customer data inline with each payment; there is no standalone customer resource.
        Customers = new UnsupportedCustomerOperations(Info);
    }

    public ProviderInfo Info { get; }
    public IChargeOperations Charges { get; }
    public IRefundOperations Refunds { get; }
    public ICustomerOperations Customers { get; }
}
