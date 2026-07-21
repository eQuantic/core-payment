using eQuantic.Mapper;
using eQuantic.Payment.MercadoPago.Payments.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.MercadoPago.Payments.Mapping;

/// <summary>Maps a Mercado Pago Payments API payment to the unified <see cref="Charge"/>.</summary>
public sealed class MercadoPagoChargeMapper : IMapper<MercadoPagoPaymentResponse, Charge>
{
    public Charge? Map(MercadoPagoPaymentResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        var method = MercadoPagoPaymentWire.FromPaymentTypeId(source.PaymentTypeId);
        var currency = source.CurrencyId ?? Money.DefaultCurrency;

        return new Charge
        {
            Id = source.Id.ToString(),
            ReferenceId = source.ExternalReference,
            Status = MercadoPagoStatusMapper.FromPayments(source.Status, source.StatusDetail),
            ProviderStatus = source.StatusDetail ?? source.Status,
            Amount = Money.FromDecimal(source.TransactionAmount, currency),
            Method = method,
            CreatedAt = source.DateCreated,
            Metadata = source.Metadata,
            Pix = method == PaymentMethodType.Pix && source.PointOfInteraction?.TransactionData is { } tx
                ? new PixOutput
                {
                    QrCode = tx.QrCode,
                    QrCodeImageUrl = tx.QrCodeBase64 is { Length: > 0 } b64 ? $"data:image/png;base64,{b64}" : tx.TicketUrl,
                    ExpiresAt = source.DateOfExpiration,
                }
                : null,
            Boleto = method == PaymentMethodType.Boleto && source.TransactionDetails is { } td
                ? new BoletoOutput
                {
                    Barcode = td.Barcode?.Content,
                    DigitableLine = td.DigitableLine,
                    Url = td.ExternalResourceUrl,
                    DueDate = source.DateOfExpiration is { } d ? DateOnly.FromDateTime(d.Date) : null,
                }
                : null,
            Card = method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard
                ? new CardOutput
                {
                    Brand = source.PaymentMethodId,
                    Last4 = source.Card?.LastFourDigits,
                    AuthorizationCode = source.AuthorizationCode,
                    Installments = source.Installments is null or <= 0 ? 1 : source.Installments.Value,
                }
                : null,
        };
    }

    public Charge? Map(MercadoPagoPaymentResponse? source, Charge? destination) => Map(source);
}
