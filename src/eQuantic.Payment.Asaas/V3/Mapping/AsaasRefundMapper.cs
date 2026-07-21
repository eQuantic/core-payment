using eQuantic.Mapper;
using eQuantic.Payment.Asaas.V3.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Asaas.V3.Mapping;

/// <summary>
/// Maps the Asaas v3 payment returned by the refund endpoint to the unified <see cref="Refund"/>. Asaas has no
/// distinct refund id, so the payment id is used for both <see cref="Refund.Id"/> and <see cref="Refund.ChargeId"/>.
/// </summary>
public sealed class AsaasRefundMapper : IMapper<AsaasPaymentResponse, Refund>
{
    public Refund? Map(AsaasPaymentResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        var latest = source.Refunds is { Count: > 0 } refunds ? refunds[^1] : null;

        return new Refund
        {
            Id = source.Id,
            ChargeId = source.Id,
            Amount = latest?.Value is { } value ? Money.FromDecimal(value) : null,
            Status = AsaasStatusMapper.FromPayment(source.Status),
            ProviderStatus = source.Status,
            CreatedAt = AsaasPaymentWire.ParseDateTime(latest?.DateCreated ?? source.DateCreated),
        };
    }

    public Refund? Map(AsaasPaymentResponse? source, Refund? destination) => Map(source);
}
