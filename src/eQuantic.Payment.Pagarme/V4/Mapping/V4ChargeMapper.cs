using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Pagarme.V4.Models;

namespace eQuantic.Payment.Pagarme.V4.Mapping;

/// <summary>Maps a legacy Pagar.me v4 transaction to the unified <see cref="Charge"/>.</summary>
public sealed class V4ChargeMapper : IMapper<V4TransactionResponse, Charge>
{
    public Charge? Map(V4TransactionResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        var method = PagarmeV4Wire.FromPaymentMethod(source.PaymentMethod);

        return new Charge
        {
            Id = source.Id.ToString(),
            ReferenceId = source.ReferenceKey,
            Status = PagarmeStatusMapper.FromV4(source.Status),
            ProviderStatus = source.Status,
            Amount = Money.FromCents(source.Amount),
            Method = method,
            CreatedAt = source.DateCreated,
            Metadata = source.Metadata,
            Pix = method == PaymentMethodType.Pix
                ? new PixOutput { QrCode = source.PixQrCode, ExpiresAt = source.PixExpirationDate }
                : null,
            Boleto = method == PaymentMethodType.Boleto
                ? new BoletoOutput
                {
                    Barcode = source.BoletoBarcode,
                    Url = source.BoletoUrl,
                    DueDate = source.BoletoExpirationDate is { } d ? DateOnly.FromDateTime(d.Date) : null,
                }
                : null,
            Card = method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard
                ? new CardOutput
                {
                    Brand = source.CardBrand,
                    Last4 = source.CardLastDigits,
                    AuthorizationCode = source.AuthorizationCode,
                    Installments = source.Installments is null or <= 0 ? 1 : source.Installments.Value,
                }
                : null,
        };
    }

    public Charge? Map(V4TransactionResponse? source, Charge? destination) => Map(source);
}
