using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Cielo.V3;

/// <summary>Unified <see cref="IPaymentProvider"/> adapter over the Cielo API 3.0 sales resource.</summary>
public sealed class CieloV3Provider : IPaymentProvider
{
    public CieloV3Provider(CieloClient client, IMapperFactory mappers)
    {
        Info = new ProviderInfo(CieloDefaults.ProviderName, CieloDefaults.VersionString(CieloApiVersion.V3));
        var operations = new CieloV3Operations(client, Info, mappers);
        Charges = operations;
        Refunds = operations;
        // Cielo embeds the customer inline with each sale; there is no standalone customer resource.
        Customers = new UnsupportedCustomerOperations(Info);
    }

    public ProviderInfo Info { get; }
    public IChargeOperations Charges { get; }
    public IRefundOperations Refunds { get; }
    public ICustomerOperations Customers { get; }
}
