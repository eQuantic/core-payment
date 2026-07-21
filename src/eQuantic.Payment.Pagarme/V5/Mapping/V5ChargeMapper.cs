using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Pagarme.V5.Models;

namespace eQuantic.Payment.Pagarme.V5.Mapping;

/// <summary>Maps a Pagar.me v5 charge to the unified <see cref="Charge"/>.</summary>
public sealed class V5ChargeMapper : IMapper<V5ChargeResponse, Charge>
{
    public Charge? Map(V5ChargeResponse? source) => Map(source, referenceId: null, metadata: null);

    public Charge? Map(V5ChargeResponse? source, Charge? destination) => Map(source);

    /// <summary>Maps a charge, optionally overriding the reference id and metadata from the parent order.</summary>
    public Charge? Map(V5ChargeResponse? source, string? referenceId, IReadOnlyDictionary<string, string>? metadata)
    {
        if (source is null)
        {
            return null;
        }

        var tx = source.LastTransaction;
        var method = PagarmeV5Wire.FromPaymentMethod(source.PaymentMethod);

        return new Charge
        {
            Id = source.Id,
            ReferenceId = referenceId ?? source.Code,
            Status = PagarmeStatusMapper.FromV5(source.Status, tx?.Status),
            ProviderStatus = tx?.Status ?? source.Status,
            Amount = Money.FromCents(source.Amount),
            Method = method,
            CreatedAt = source.CreatedAt,
            Metadata = metadata ?? source.Metadata,
            Pix = method == PaymentMethodType.Pix && tx is not null
                ? new PixOutput { QrCode = tx.QrCode, QrCodeImageUrl = tx.QrCodeUrl, ExpiresAt = tx.ExpiresAt }
                : null,
            Boleto = method == PaymentMethodType.Boleto && tx is not null
                ? new BoletoOutput
                {
                    Barcode = tx.Barcode,
                    DigitableLine = tx.Line,
                    Url = tx.Pdf ?? tx.Url,
                    DueDate = tx.DueAt is { } d ? DateOnly.FromDateTime(d.Date) : null,
                }
                : null,
            Card = method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard && tx is not null
                ? new CardOutput
                {
                    Brand = tx.Card?.Brand,
                    Last4 = tx.Card?.LastFourDigits,
                    AuthorizationCode = tx.AcquirerAuthCode,
                    Installments = tx.Installments <= 0 ? 1 : tx.Installments,
                }
                : null,
        };
    }
}
