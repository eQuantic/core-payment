using System.Globalization;
using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.PagSeguro.Orders.Models;

namespace eQuantic.Payment.PagSeguro.Orders.Mapping;

/// <summary>Maps a PagSeguro card/boleto charge to the unified <see cref="Charge"/>.</summary>
public sealed class PagSeguroChargeMapper : IMapper<PagSeguroChargeResponse, Charge>
{
    public Charge? Map(PagSeguroChargeResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        var pm = source.PaymentMethod;
        var method = FromType(pm?.Type);

        return new Charge
        {
            Id = source.Id,
            ReferenceId = source.ReferenceId,
            Status = PagSeguroStatusMapper.FromCharge(source.Status),
            ProviderStatus = source.Status,
            Amount = Money.FromCents(source.Amount?.Value ?? 0, source.Amount?.Currency ?? Money.DefaultCurrency),
            Method = method,
            CreatedAt = source.CreatedAt,
            Boleto = method == PaymentMethodType.Boleto && pm?.Boleto is { } boleto
                ? new BoletoOutput
                {
                    Barcode = boleto.Barcode,
                    DigitableLine = boleto.FormattedBarcode ?? boleto.Barcode,
                    Url = FindLink(source.Links, "application/pdf"),
                    DueDate = ParseDate(boleto.DueDate),
                }
                : null,
            Card = method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard && pm?.Card is { } card
                ? new CardOutput
                {
                    Brand = card.Brand,
                    Last4 = card.LastDigits,
                    AuthorizationCode = source.PaymentResponse?.Reference,
                    Installments = pm.Installments is null or <= 0 ? 1 : pm.Installments.Value,
                }
                : null,
        };
    }

    public Charge? Map(PagSeguroChargeResponse? source, Charge? destination) => Map(source);

    internal static PaymentMethodType FromType(string? type) => type?.ToUpperInvariant() switch
    {
        "CREDIT_CARD" => PaymentMethodType.CreditCard,
        "DEBIT_CARD" => PaymentMethodType.DebitCard,
        "BOLETO" => PaymentMethodType.Boleto,
        _ => PaymentMethodType.CreditCard,
    };

    internal static string? FindLink(List<PagSeguroLink>? links, string media)
        => links?.FirstOrDefault(l => string.Equals(l.Media, media, StringComparison.OrdinalIgnoreCase))?.Href;

    internal static string? FindLinkByRel(List<PagSeguroLink>? links, string rel)
        => links?.FirstOrDefault(l => string.Equals(l.Rel, rel, StringComparison.OrdinalIgnoreCase))?.Href;

    private static DateOnly? ParseDate(string? date)
        => DateOnly.TryParse(date, CultureInfo.InvariantCulture, out var d) ? d : null;
}
