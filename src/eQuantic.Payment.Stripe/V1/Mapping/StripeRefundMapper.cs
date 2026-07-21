using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>Maps a Stripe refund to the unified <see cref="Refund"/>.</summary>
public sealed class StripeRefundMapper : IMapper<StripeRefund, Refund>
{
    public Refund? Map(StripeRefund? source)
        => source is null
            ? null
            : new Refund
            {
                Id = source.Id,
                ChargeId = source.PaymentIntent ?? source.Charge ?? string.Empty,
                Amount = Money.FromCents(source.Amount, (source.Currency ?? "brl").ToUpperInvariant()),
                Status = StripeStatusMapper.FromRefund(source.Status),
                ProviderStatus = source.Status,
                CreatedAt = source.Created is { } c ? DateTimeOffset.FromUnixTimeSeconds(c) : null,
            };

    public Refund? Map(StripeRefund? source, Refund? destination) => Map(source);
}
