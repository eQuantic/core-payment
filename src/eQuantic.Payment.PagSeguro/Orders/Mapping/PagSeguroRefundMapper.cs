using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.PagSeguro.Orders.Models;

namespace eQuantic.Payment.PagSeguro.Orders.Mapping;

/// <summary>
/// Maps the charge returned by a cancel/refund (<c>POST /charges/{id}/cancel</c>) to the unified <see cref="Refund"/>.
/// PagSeguro reports both void and refund as <c>CANCELED</c>; the refunded amount in <c>summary</c> distinguishes them.
/// </summary>
public sealed class PagSeguroRefundMapper : IMapper<PagSeguroChargeResponse, Refund>
{
    public Refund? Map(PagSeguroChargeResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        var total = source.Amount?.Value ?? 0;
        var refunded = source.Amount?.Summary?.Refunded ?? 0;
        var status = refunded switch
        {
            0 => PaymentStatus.Canceled,
            _ when refunded >= total => PaymentStatus.Refunded,
            _ => PaymentStatus.PartiallyRefunded,
        };

        return new Refund
        {
            Id = source.Id,
            ChargeId = source.Id,
            Amount = Money.FromCents(refunded > 0 ? refunded : total, source.Amount?.Currency ?? Money.DefaultCurrency),
            Status = status,
            ProviderStatus = source.Status,
            CreatedAt = source.PaidAt ?? source.CreatedAt,
        };
    }

    public Refund? Map(PagSeguroChargeResponse? source, Refund? destination) => Map(source);
}
