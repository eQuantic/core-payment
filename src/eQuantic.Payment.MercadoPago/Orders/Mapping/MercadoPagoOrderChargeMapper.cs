using eQuantic.Mapper;
using eQuantic.Payment.MercadoPago.Orders.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.MercadoPago.Orders.Mapping;

/// <summary>
/// Maps a Mercado Pago order to the unified <see cref="Charge"/>. The unified <c>Charge.Id</c> is the order id
/// (<c>ORD...</c>) so that get/capture/cancel/refund operate on the order; PIX/boleto/card artifacts come from
/// the first nested payment's <c>payment_method</c>.
/// </summary>
public sealed class MercadoPagoOrderChargeMapper : IMapper<MercadoPagoOrderResponse, Charge>
{
    public Charge? Map(MercadoPagoOrderResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        var payment = source.Transactions?.Payments?.FirstOrDefault();
        var pm = payment?.PaymentMethod;
        var method = MercadoPagoOrderWire.FromType(pm?.Type);
        var currency = source.Currency ?? Money.DefaultCurrency;

        return new Charge
        {
            Id = source.Id,
            ReferenceId = source.ExternalReference,
            Status = MercadoPagoStatusMapper.FromOrders(source.Status, source.StatusDetail),
            ProviderStatus = source.StatusDetail ?? source.Status,
            Amount = Money.FromDecimal(MercadoPagoOrderWire.ParseAmount(source.TotalAmount), currency),
            Method = method,
            CreatedAt = source.CreatedDate,
            Pix = method == PaymentMethodType.Pix && pm is not null
                ? new PixOutput
                {
                    QrCode = pm.QrCode,
                    QrCodeImageUrl = pm.QrCodeBase64 is { Length: > 0 } b64 ? $"data:image/png;base64,{b64}" : pm.TicketUrl,
                    ExpiresAt = payment?.DateOfExpiration,
                }
                : null,
            Boleto = method == PaymentMethodType.Boleto && pm is not null
                ? new BoletoOutput
                {
                    Barcode = pm.BarcodeContent,
                    DigitableLine = pm.DigitableLine,
                    Url = pm.TicketUrl,
                    DueDate = payment?.DateOfExpiration is { } d ? DateOnly.FromDateTime(d.Date) : null,
                }
                : null,
            Card = method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard && pm is not null
                ? new CardOutput
                {
                    Brand = pm.Id,
                    Installments = pm.Installments is null or <= 0 ? 1 : pm.Installments.Value,
                }
                : null,
        };
    }

    public Charge? Map(MercadoPagoOrderResponse? source, Charge? destination) => Map(source);
}
