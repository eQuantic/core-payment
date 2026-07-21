using eQuantic.Mapper;
using eQuantic.Payment.MercadoPago.Orders.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.MercadoPago.Orders.Mapping;

/// <summary>Maps the order returned by an Orders API refund to the unified <see cref="Refund"/>.</summary>
public sealed class MercadoPagoOrderRefundMapper : IMapper<MercadoPagoOrderResponse, Refund>
{
    public Refund? Map(MercadoPagoOrderResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        var refund = source.Transactions?.Refunds?.FirstOrDefault();
        return new Refund
        {
            Id = refund?.Id ?? source.Id,
            ChargeId = source.Id,
            Amount = Money.FromDecimal(MercadoPagoOrderWire.ParseAmount(refund?.Amount ?? source.TotalPaidAmount)),
            Status = MercadoPagoStatusMapper.FromOrders(refund?.Status ?? source.Status, source.StatusDetail),
            ProviderStatus = refund?.Status ?? source.StatusDetail ?? source.Status,
            CreatedAt = source.LastUpdatedDate,
        };
    }

    public Refund? Map(MercadoPagoOrderResponse? source, Refund? destination) => Map(source);
}
