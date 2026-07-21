using eQuantic.Mapper;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Pagarme.V5.Models;

namespace eQuantic.Payment.Pagarme.V5.Mapping;

/// <summary>
/// Maps a Pagar.me v5 order (returned by <c>POST /orders</c>) to the unified <see cref="Charge"/>,
/// projecting its first charge and propagating the order code/metadata as reference id/metadata.
/// </summary>
public sealed class V5OrderMapper(IMapperFactory mapperFactory) : IMapper<V5OrderResponse, Charge>
{
    public Charge? Map(V5OrderResponse? source)
    {
        var charge = source?.Charges?.FirstOrDefault();
        if (source is null || charge is null)
        {
            return null;
        }

        var chargeMapper = (V5ChargeMapper)mapperFactory.GetMapper<V5ChargeResponse, Charge>();
        return chargeMapper.Map(charge, source.Code ?? charge.Code, source.Metadata ?? charge.Metadata);
    }

    public Charge? Map(V5OrderResponse? source, Charge? destination) => Map(source);
}
