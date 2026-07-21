using eQuantic.Mapper;
using eQuantic.Payment.Adyen.V71.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Adyen.V71.Mapping;

/// <summary>
/// Maps a refund acknowledgement to the unified <see cref="Refund"/>. The refund is asynchronous, so the ack
/// is mapped with <see cref="PaymentStatus.Processing"/>; the final state (REFUND / REFUND_FAILED) arrives via
/// a webhook. <see cref="Refund.Id"/> is the modification's own pspReference; <see cref="Refund.ChargeId"/> is
/// the original payment pspReference.
/// </summary>
public sealed class AdyenRefundMapper : IMapper<AdyenModificationResponse, Refund>
{
    public Refund? Map(AdyenModificationResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        return new Refund
        {
            Id = source.PspReference ?? string.Empty,
            ChargeId = source.PaymentPspReference ?? string.Empty,
            Amount = source.Amount is { } amount ? Money.FromCents(amount.Value, amount.Currency) : null,
            Status = PaymentStatus.Processing,
            ProviderStatus = source.Status,
        };
    }

    public Refund? Map(AdyenModificationResponse? source, Refund? destination) => Map(source);
}
