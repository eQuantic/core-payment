using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;

namespace eQuantic.Payment.Asaas.V3;

/// <summary>Unified <see cref="IPaymentProvider"/> adapter over the Asaas v3 API.</summary>
public sealed class AsaasV3Provider : IPaymentProvider
{
    public AsaasV3Provider(AsaasV3Client client, IMapperFactory mappers)
    {
        Info = new ProviderInfo(AsaasDefaults.ProviderName, AsaasDefaults.VersionString(AsaasApiVersion.V3));
        var operations = new AsaasV3Operations(client, Info, mappers);
        Charges = operations;
        Refunds = operations;
        Customers = new AsaasV3CustomerOperations(client, Info, mappers);
    }

    public ProviderInfo Info { get; }
    public IChargeOperations Charges { get; }
    public IRefundOperations Refunds { get; }
    public ICustomerOperations Customers { get; }
}
