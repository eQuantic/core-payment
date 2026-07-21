using eQuantic.Mapper;
using eQuantic.Payment.Asaas.V3.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Asaas.V3.Mapping;

/// <summary>
/// Maps the Asaas v3 delete response (<c>{ deleted, id }</c>) to a canceled <see cref="Charge"/>. The delete
/// endpoint does not echo the full payment, so only the id and a canceled status are available.
/// </summary>
public sealed class AsaasCancelMapper : IMapper<AsaasDeleteResponse, Charge>
{
    public Charge? Map(AsaasDeleteResponse? source)
        => source is null
            ? null
            : new Charge
            {
                Id = source.Id,
                Status = source.Deleted ? PaymentStatus.Canceled : PaymentStatus.Unknown,
                ProviderStatus = source.Deleted ? "deleted" : null,
            };

    public Charge? Map(AsaasDeleteResponse? source, Charge? destination) => Map(source);
}
