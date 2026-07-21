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
            Status = StripeStatusMapper.FromPaymentIntent(source.Status),
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
                    DueDate = boleto.ExpiresAt is { } boletoExp ? DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(boletoExp).Date) : null,
                }
                : null,
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

    private static PaymentMethodType FromPaymentMethodType(string? type) => type?.ToLowerInvariant() switch
    {
        "card" => PaymentMethodType.CreditCard,
        "pix" => PaymentMethodType.Pix,
        "boleto" => PaymentMethodType.Boleto,
        _ => PaymentMethodType.CreditCard,
    };
}
