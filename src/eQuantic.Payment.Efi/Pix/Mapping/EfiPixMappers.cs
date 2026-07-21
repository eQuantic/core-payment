using System.Globalization;
using eQuantic.Mapper;
using eQuantic.Payment.Efi.Pix.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Efi.Pix.Mapping;

/// <summary>Maps the unified <see cref="CreateChargeRequest"/> to an Efí Pix charge request (without <c>chave</c>, set by the operation).</summary>
public sealed class EfiPixCobRequestMapper : IMapper<CreateChargeRequest, EfiPixCobRequest>
{
    public EfiPixCobRequest? Map(CreateChargeRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        return new EfiPixCobRequest
        {
            Calendario = new EfiPixCalendario { Expiracao = (int)(source.Pix?.ExpiresIn ?? TimeSpan.FromHours(1)).TotalSeconds },
            Valor = new EfiPixValor { Original = source.Amount.Amount.ToString("0.00", CultureInfo.InvariantCulture) },
            Devedor = MapDevedor(source.Customer),
            SolicitacaoPagador = source.Description,
        };
    }

    public EfiPixCobRequest? Map(CreateChargeRequest? source, EfiPixCobRequest? destination) => Map(source);

    private static EfiPixDevedor? MapDevedor(CustomerRequest? customer)
    {
        if (customer?.DocumentDigits is not { } doc)
        {
            return null;
        }

        var devedor = new EfiPixDevedor { Nome = customer.Name };
        if (customer.DocumentType == DocumentType.Cnpj)
        {
            devedor.Cnpj = doc;
        }
        else
        {
            devedor.Cpf = doc;
        }

        return devedor;
    }
}

/// <summary>Context for <see cref="EfiPixChargeMapper"/>, carrying the QR image fetched from a separate endpoint.</summary>
public sealed class EfiPixChargeContext
{
    public string? QrCodeImageUrl { get; set; }
}

/// <summary>Maps an Efí Pix <c>cob</c> (plus the separately-fetched QR image) to the unified <see cref="Charge"/>.</summary>
public sealed class EfiPixChargeMapper : IMapper<EfiPixCobResponse, Charge, EfiPixChargeContext>
{
    public EfiPixChargeContext? Context { get; set; }

    public Charge? Map(EfiPixCobResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        var created = source.Calendario?.Criacao;
        var expiresAt = created is { } c && source.Calendario is { } cal ? c.AddSeconds(cal.Expiracao) : (DateTimeOffset?)null;

        return new Charge
        {
            Id = source.Txid ?? string.Empty,
            Status = EfiStatusMapper.FromPix(source.Status),
            ProviderStatus = source.Status,
            Amount = Money.FromDecimal(ParseAmount(source.Valor?.Original)),
            Method = PaymentMethodType.Pix,
            CreatedAt = created,
            Pix = new PixOutput
            {
                QrCode = source.PixCopiaECola,
                QrCodeImageUrl = Context?.QrCodeImageUrl,
                ExpiresAt = expiresAt,
            },
        };
    }

    public Charge? Map(EfiPixCobResponse? source, Charge? destination) => Map(source);

    internal static decimal ParseAmount(string? value)
        => decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;
}

/// <summary>Maps an Efí Pix refund (<c>devolucao</c>) to the unified <see cref="Refund"/> (charge id set by the operation).</summary>
public sealed class EfiPixRefundMapper : IMapper<EfiPixDevolucao, Refund>
{
    public Refund? Map(EfiPixDevolucao? source)
        => source is null
            ? null
            : new Refund
            {
                Id = source.Id ?? string.Empty,
                ChargeId = string.Empty,
                Amount = Money.FromDecimal(EfiPixChargeMapper.ParseAmount(source.Valor)),
                Status = source.Status?.ToUpperInvariant() switch
                {
                    "DEVOLVIDO" => PaymentStatus.Refunded,
                    "EM_PROCESSAMENTO" => PaymentStatus.Processing,
                    "NAO_REALIZADO" => PaymentStatus.Failed,
                    _ => PaymentStatus.Unknown,
                },
                ProviderStatus = source.Status,
            };

    public Refund? Map(EfiPixDevolucao? source, Refund? destination) => Map(source);
}
