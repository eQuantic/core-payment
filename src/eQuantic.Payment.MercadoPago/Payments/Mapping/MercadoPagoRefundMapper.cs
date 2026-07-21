using eQuantic.Mapper;
using eQuantic.Payment.MercadoPago.Payments.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.MercadoPago.Payments.Mapping;

/// <summary>Maps a Mercado Pago refund to the unified <see cref="Refund"/>.</summary>
public sealed class MercadoPagoRefundMapper : IMapper<MercadoPagoRefundResponse, Refund>
{
    public Refund? Map(MercadoPagoRefundResponse? source)
        => source is null
            ? null
            : new Refund
            {
                Id = source.Id.ToString(),
                ChargeId = source.PaymentId.ToString(),
                Amount = Money.FromDecimal(source.Amount),
                Status = MercadoPagoStatusMapper.FromPayments(source.Status),
                ProviderStatus = source.Status,
                CreatedAt = source.DateCreated,
            };

    public Refund? Map(MercadoPagoRefundResponse? source, Refund? destination) => Map(source);
}
