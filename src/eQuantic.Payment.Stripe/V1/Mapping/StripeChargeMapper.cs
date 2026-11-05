using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>Maps a Stripe PaymentIntent to the unified <see cref="Charge"/>.</summary>
public sealed class StripeChargeMapper : IMapper<StripePaymentIntent, Charge>
{
    public Charge? Map(StripePaymentIntent? source)
    {
        if (source is null)
        {
            return null;
        }

        var method = FromPaymentMethodType(source.PaymentMethodTypes?.FirstOrDefault());
        var card = source.LatestCharge?.PaymentMethodDetails?.Card;

        return new Charge
        {
            Id = source.Id,
            ReferenceId = source.Metadata?.GetValueOrDefault("reference_id"),
            Status = StripeStatusMapper.FromPaymentIntent(source.Status, source.LastPaymentError?.Code, source.LastPaymentError is not null),
            ProviderStatus = source.Status,
            Amount = Money.FromCents(source.Amount, (source.Currency ?? "brl").ToUpperInvariant()),
            Method = method,
            CreatedAt = source.Created is { } c ? DateTimeOffset.FromUnixTimeSeconds(c) : null,
            Metadata = source.Metadata,
            Pix = method == PaymentMethodType.Pix && source.NextAction?.PixDisplayQrCode is { } pix
                ? new PixOutput
                {
                    QrCode = pix.Data,
                    QrCodeImageUrl = pix.ImageUrlPng ?? pix.ImageUrlSvg ?? pix.HostedInstructionsUrl,
                    ExpiresAt = pix.ExpiresAt is { } pixExp ? DateTimeOffset.FromUnixTimeSeconds(pixExp) : null,
                }
                : null,
            Boleto = method == PaymentMethodType.Boleto && source.NextAction?.BoletoDisplayDetails is { } boleto
                ? new BoletoOutput
                {
                    DigitableLine = boleto.Number,
                    Url = boleto.Pdf ?? boleto.HostedVoucherUrl,
                    PdfUrl = boleto.Pdf,
                    HostedUrl = boleto.HostedVoucherUrl,
                    // The voucher expires at the end of a São Paulo day, which is already the next day in UTC.
                    DueDate = boleto.ExpiresAt is { } boletoExp ? StripeBoleto.DayOf(boletoExp) : null,
                    ExpiresAt = boleto.ExpiresAt is { } expiresAt ? DateTimeOffset.FromUnixTimeSeconds(expiresAt) : null,
                }
                : null,
            Multibanco = method == PaymentMethodType.Multibanco ? Multibanco(source) : null,
            Card = method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard
                ? new CardOutput
                {
                    Brand = card?.Brand,
                    Last4 = card?.Last4,
                    AuthorizationCode = card?.AuthorizationCode,
                    Installments = card?.Installments?.Plan?.Count is { } n and > 0 ? n : 1,
                }
                : null,
        };
    }

    public Charge? Map(StripePaymentIntent? source, Charge? destination) => Map(source);

    /// <summary>
    /// The voucher, from <c>next_action</c> while it is payable, else from the latest charge, which keeps the entity and
    /// reference once paid.
    /// </summary>
    private static MultibancoOutput? Multibanco(StripePaymentIntent source)
    {
        if (source.NextAction?.MultibancoDisplayDetails is { } voucher)
        {
            return new MultibancoOutput
            {
                Entity = voucher.Entity,
                Reference = voucher.Reference,
                ExpiresAt = voucher.ExpiresAt is { } expiresAt ? DateTimeOffset.FromUnixTimeSeconds(expiresAt) : null,
                HostedUrl = voucher.HostedVoucherUrl,
            };
        }

        return source.LatestCharge?.PaymentMethodDetails?.Multibanco is { } paid
            ? new MultibancoOutput { Entity = paid.Entity, Reference = paid.Reference }
            : null;
    }

    private static PaymentMethodType FromPaymentMethodType(string? type) => type?.ToLowerInvariant() switch
    {
        "card" => PaymentMethodType.CreditCard,
        "pix" => PaymentMethodType.Pix,
        "boleto" => PaymentMethodType.Boleto,
        "mb_way" => PaymentMethodType.MbWay,
        "multibanco" => PaymentMethodType.Multibanco,
        _ => PaymentMethodType.CreditCard,
    };
}
