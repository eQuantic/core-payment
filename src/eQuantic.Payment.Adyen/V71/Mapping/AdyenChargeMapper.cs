using System.Globalization;
using eQuantic.Mapper;
using eQuantic.Payment.Adyen.V71.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Adyen.V71.Mapping;

/// <summary>
/// Maps an Adyen <c>/payments</c> response to the unified <see cref="Charge"/>. The amount and method come
/// from <see cref="AdyenChargeContext"/> (the response omits the amount) and PIX/boleto/card artifacts are
/// read from the <c>action</c> and <c>additionalData</c>.
/// </summary>
public sealed class AdyenChargeMapper : IMapper<AdyenPaymentResponse, Charge, AdyenChargeContext>
{
    public AdyenChargeContext? Context { get; set; }

    public Charge? Map(AdyenPaymentResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        var method = Context?.Method ?? InferMethod(source);
        var action = source.Action;

        return new Charge
        {
            Id = source.PspReference is { Length: > 0 } psp ? psp : source.MerchantReference ?? string.Empty,
            ReferenceId = source.MerchantReference,
            Status = AdyenStatusMapper.FromResultCode(source.ResultCode),
            ProviderStatus = source.ResultCode,
            Amount = ResolveAmount(source),
            Method = method,
            Pix = method == PaymentMethodType.Pix && action is not null
                ? new PixOutput
                {
                    QrCode = action.QrCodeData,
                    QrCodeImageUrl = action.Url,
                    ExpiresAt = ParseOffset(GetAdditional(source, "pix.expirationDate")),
                }
                : null,
            Boleto = method == PaymentMethodType.Boleto && action is not null
                ? new BoletoOutput
                {
                    Barcode = action.Reference ?? action.RawData,
                    DigitableLine = action.Reference ?? action.RawData,
                    Url = action.DownloadUrl,
                    DueDate = ParseDate(action.ExpiresAt),
                }
                : null,
            Card = method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard
                ? new CardOutput
                {
                    Brand = source.PaymentMethod?.Brand,
                    Last4 = GetAdditional(source, "cardSummary"),
                    AuthorizationCode = GetAdditional(source, "authCode"),
                    Installments = 1,
                }
                : null,
        };
    }

    public Charge? Map(AdyenPaymentResponse? source, Charge? destination) => Map(source);

    private Money ResolveAmount(AdyenPaymentResponse source)
    {
        if (source.Amount is { } amount)
        {
            return Money.FromCents(amount.Value, amount.Currency);
        }

        if (source.Action?.TotalAmount is { } total)
        {
            return Money.FromCents(total.Value, total.Currency);
        }

        return Context?.Amount ?? Money.FromCents(0);
    }

    private static PaymentMethodType InferMethod(AdyenPaymentResponse source)
    {
        if (string.Equals(source.Action?.Type, "qrCode", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentMethodType.Pix;
        }

        if (string.Equals(source.Action?.Type, "voucher", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentMethodType.Boleto;
        }

        return source.PaymentMethod?.Type?.ToLowerInvariant() switch
        {
            "pix" => PaymentMethodType.Pix,
            "boletobancario" => PaymentMethodType.Boleto,
            _ => PaymentMethodType.CreditCard,
        };
    }

    private static string? GetAdditional(AdyenPaymentResponse source, string key)
        => source.AdditionalData is { } data && data.TryGetValue(key, out var value) ? value : null;

    private static DateTimeOffset? ParseOffset(string? value)
        => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) ? parsed : null;

    private static DateOnly? ParseDate(string? value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed)
            ? DateOnly.FromDateTime(parsed)
            : null;
}
