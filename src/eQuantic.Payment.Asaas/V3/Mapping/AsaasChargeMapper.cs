using eQuantic.Mapper;
using eQuantic.Payment.Asaas.V3.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Asaas.V3.Mapping;

/// <summary>
/// Maps an Asaas v3 payment (with its follow-up PIX QR and boleto identification-field responses) to the
/// unified <see cref="Charge"/>.
/// </summary>
public sealed class AsaasChargeMapper : IMapper<AsaasPaymentResult, Charge>
{
    public Charge? Map(AsaasPaymentResult? source)
    {
        if (source is null)
        {
            return null;
        }

        var payment = source.Payment;
        var method = AsaasPaymentWire.FromBillingType(payment.BillingType);

        return new Charge
        {
            Id = payment.Id,
            ReferenceId = payment.ExternalReference,
            Status = AsaasStatusMapper.FromPayment(payment.Status),
            ProviderStatus = payment.Status,
            Amount = Money.FromDecimal(payment.Value ?? 0m),
            Method = method,
            CreatedAt = AsaasPaymentWire.ParseDateTime(payment.DateCreated),
            Pix = method == PaymentMethodType.Pix && source.PixQrCode is { } pix
                ? new PixOutput
                {
                    QrCode = pix.Payload,
                    QrCodeImageUrl = pix.EncodedImage is { Length: > 0 } image ? $"data:image/png;base64,{image}" : null,
                    ExpiresAt = AsaasPaymentWire.ParseDateTime(pix.ExpirationDate),
                }
                : null,
            Boleto = method == PaymentMethodType.Boleto
                ? new BoletoOutput
                {
                    Barcode = source.IdentificationField?.BarCode,
                    DigitableLine = source.IdentificationField?.IdentificationField,
                    Url = payment.BankSlipUrl,
                    DueDate = AsaasPaymentWire.ParseDate(payment.DueDate),
                }
                : null,
            Card = method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard
                ? new CardOutput
                {
                    Brand = payment.CreditCard?.CreditCardBrand,
                    Last4 = payment.CreditCard?.CreditCardNumber,
                }
                : null,
        };
    }

    public Charge? Map(AsaasPaymentResult? source, Charge? destination) => Map(source);
}
