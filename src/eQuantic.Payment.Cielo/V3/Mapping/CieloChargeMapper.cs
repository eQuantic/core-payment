using System.Globalization;
using eQuantic.Mapper;
using eQuantic.Payment.Cielo.V3.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Cielo.V3.Mapping;

/// <summary>Maps a Cielo sale (create/get response) to the unified <see cref="Charge"/>.</summary>
public sealed class CieloChargeMapper : IMapper<CieloSaleResponse, Charge>
{
    public Charge? Map(CieloSaleResponse? source)
    {
        var payment = source?.Payment;
        if (payment is null)
        {
            return null;
        }

        var method = CieloV3Wire.FromPaymentType(payment.Type);
        var card = payment.CreditCard ?? payment.DebitCard;

        return new Charge
        {
            Id = payment.PaymentId ?? string.Empty,
            ReferenceId = source?.MerchantOrderId,
            Status = CieloStatusMapper.FromCode(payment.Status),
            ProviderStatus = payment.Status.ToString(CultureInfo.InvariantCulture),
            Amount = Money.FromCents(payment.Amount, payment.Currency ?? Money.DefaultCurrency),
            Method = method,
            CreatedAt = ParseTimestamp(payment.ReceivedDate),
            Pix = method == PaymentMethodType.Pix
                ? new PixOutput
                {
                    QrCode = payment.QrCodeString,
                    QrCodeImageUrl = payment.QrCodeBase64Image is { Length: > 0 } b64
                        ? $"data:image/png;base64,{b64}"
                        : null,
                }
                : null,
            Boleto = method == PaymentMethodType.Boleto
                ? new BoletoOutput
                {
                    Barcode = payment.BarCodeNumber,
                    DigitableLine = payment.DigitableLine,
                    Url = payment.Url,
                    DueDate = ParseDate(payment.ExpirationDate),
                }
                : null,
            Card = method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard
                ? new CardOutput
                {
                    Brand = card?.Brand,
                    Last4 = LastFour(card?.CardNumber),
                    AuthorizationCode = payment.AuthorizationCode,
                    Installments = payment.Installments is null or <= 0 ? 1 : payment.Installments.Value,
                }
                : null,
        };
    }

    public Charge? Map(CieloSaleResponse? source, Charge? destination) => Map(source);

    private static string? LastFour(string? maskedPan)
        => maskedPan is { Length: >= 4 } pan ? pan[^4..] : null;

    private static DateTimeOffset? ParseTimestamp(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d)
            ? d
            : null;

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
}
