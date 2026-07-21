using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Pagarme.V5.Models;

namespace eQuantic.Payment.Pagarme.V5.Mapping;

/// <summary>
/// Maps the Pagar.me v5 charge returned by a cancel/refund (<c>DELETE /charges/{id}</c>) to the unified <see cref="Refund"/>.
/// The refunded amount defaults to the charge amount; callers override it for partial refunds.
/// </summary>
public sealed class V5RefundMapper : IMapper<V5ChargeResponse, Refund>
{
    public Refund? Map(V5ChargeResponse? source)
        => source is null
            ? null
            : new Refund
            {
                Id = source.Id,
                ChargeId = source.Id,
                Amount = Money.FromCents(source.Amount),
                Status = PagarmeStatusMapper.FromV5(source.Status, source.LastTransaction?.Status),
                ProviderStatus = source.LastTransaction?.Status ?? source.Status,
                CreatedAt = source.CreatedAt,
            };

    public Refund? Map(V5ChargeResponse? source, Refund? destination) => Map(source);
}
