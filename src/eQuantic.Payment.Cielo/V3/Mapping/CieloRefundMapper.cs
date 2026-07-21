using System.Globalization;
using eQuantic.Mapper;
using eQuantic.Payment.Cielo.V3.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Cielo.V3.Mapping;

/// <summary>
/// Maps the flat void response (<c>PUT /1/sales/{id}/void</c>) to the unified <see cref="Refund"/>. Cielo uses the
/// void endpoint for both cancellation (was authorized, status <c>10</c>) and refund (was captured, status <c>11</c>).
/// The response echoes no id/amount, so the operations layer injects the charge id and refunded amount.
/// </summary>
public sealed class CieloRefundMapper : IMapper<CieloReturnResponse, Refund>
{
    public Refund? Map(CieloReturnResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        return new Refund
        {
            Id = string.Empty,
            ChargeId = string.Empty,
            Status = CieloStatusMapper.FromCode(source.Status),
            ProviderStatus = source.Status.ToString(CultureInfo.InvariantCulture),
        };
    }

    public Refund? Map(CieloReturnResponse? source, Refund? destination) => Map(source);
}
