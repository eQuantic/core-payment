using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.PagSeguro.Orders.Models;

namespace eQuantic.Payment.PagSeguro.Orders.Mapping;

/// <summary>
/// Maps a PagSeguro order to the unified <see cref="Charge"/>. Card/boleto orders project their first charge;
/// PIX orders (no charge) project the first QR code, using the order id as the unified <c>Charge.Id</c>.
/// </summary>
public sealed class PagSeguroOrderMapper(IMapperFactory mapperFactory) : IMapper<PagSeguroOrderResponse, Charge>
{
    public Charge? Map(PagSeguroOrderResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        var charge = source.Charges?.FirstOrDefault();
        if (charge is not null)
        {
            return mapperFactory.GetMapper<PagSeguroChargeResponse, Charge>().Map(charge);
        }

        var qr = source.QrCodes?.FirstOrDefault();
        if (qr is null)
        {
            return null;
        }

        return new Charge
        {
            Id = source.Id,
            ReferenceId = source.ReferenceId,
            Status = PaymentStatus.Pending,
            ProviderStatus = "WAITING",
            Amount = Money.FromCents(qr.Amount?.Value ?? 0, qr.Amount?.Currency ?? Money.DefaultCurrency),
            Method = PaymentMethodType.Pix,
            CreatedAt = source.CreatedAt,
            Pix = new PixOutput
            {
                QrCode = qr.Text,
                QrCodeImageUrl = PagSeguroChargeMapper.FindLinkByRel(qr.Links, "QRCODE.PNG"),
                ExpiresAt = qr.ExpirationDate,
            },
        };
    }

    public Charge? Map(PagSeguroOrderResponse? source, Charge? destination) => Map(source);
}
