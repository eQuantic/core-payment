using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Pagarme.V4.Models;

namespace eQuantic.Payment.Pagarme.V4.Mapping;

/// <summary>
/// Maps the legacy Pagar.me v4 transaction returned by a refund to the unified <see cref="Refund"/>.
/// The refunded amount defaults to the transaction amount; callers override it for partial refunds.
/// </summary>
public sealed class V4RefundMapper : IMapper<V4TransactionResponse, Refund>
{
    public Refund? Map(V4TransactionResponse? source)
        => source is null
            ? null
            : new Refund
            {
                Id = source.Id.ToString(),
                ChargeId = source.Id.ToString(),
                Amount = Money.FromCents(source.RefundedAmount ?? source.Amount),
                Status = PagarmeStatusMapper.FromV4(source.Status),
                ProviderStatus = source.Status,
                CreatedAt = source.DateCreated,
            };

    public Refund? Map(V4TransactionResponse? source, Refund? destination) => Map(source);
}
