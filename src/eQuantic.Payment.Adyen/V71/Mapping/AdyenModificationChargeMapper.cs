using eQuantic.Mapper;
using eQuantic.Payment.Adyen.V71.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Adyen.V71.Mapping;

/// <summary>
/// Maps a capture/cancel acknowledgement to the unified <see cref="Charge"/>. Modifications are asynchronous,
/// so the ack is mapped with <see cref="PaymentStatus.Processing"/>; the final state arrives via a webhook.
/// The charge keeps the original payment pspReference as its id.
/// </summary>
public sealed class AdyenModificationChargeMapper : IMapper<AdyenModificationResponse, Charge>
{
    public Charge? Map(AdyenModificationResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        return new Charge
        {
            Id = source.PaymentPspReference ?? source.PspReference ?? string.Empty,
            ReferenceId = source.Reference,
            Status = PaymentStatus.Processing,
            ProviderStatus = source.Status,
            Amount = source.Amount is { } amount ? Money.FromCents(amount.Value, amount.Currency) : Money.FromCents(0),
        };
    }

    public Charge? Map(AdyenModificationResponse? source, Charge? destination) => Map(source);
}
