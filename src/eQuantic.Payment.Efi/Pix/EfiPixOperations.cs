using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Efi.Pix.Mapping;
using eQuantic.Payment.Efi.Pix.Models;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Efi.Pix;

/// <summary>Charge and refund operations over the Efí Pix API (PIX only).</summary>
internal sealed class EfiPixOperations(EfiPixClient client, ProviderInfo info, IMapperFactory mappers, string? pixKey)
    : IChargeOperations, IRefundOperations
{
    public async Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Method != PaymentMethodType.Pix)
        {
            return PaymentResponse<Charge>.Fail(info, new PaymentError
            {
                Message = "The Efí Pix API only supports PIX. Register the 'cobrancas' version for boleto/credit card.",
            });
        }

        if (string.IsNullOrWhiteSpace(pixKey))
        {
            return PaymentResponse<Charge>.Fail(info, new PaymentError { Message = "EfiOptions.PixKey is required to create PIX charges." });
        }

        var cob = mappers.GetMapper<CreateChargeRequest, EfiPixCobRequest>().Map(request)!;
        cob.Chave = pixKey!;

        var txid = Guid.NewGuid().ToString("N"); // 32 alphanumeric chars, within the 26–35 range
        var result = await client.CreateChargeAsync(txid, cob, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, EfiErrorMapper.ToError(result), result.RawBody);
        }

        // The QR image lives on a separate endpoint keyed by the location id.
        string? qrImage = null;
        if (result.Data.Loc?.Id is { } locId)
        {
            var qr = await client.GetQrCodeAsync(locId, cancellationToken).ConfigureAwait(false);
            qrImage = qr.Data?.ImagemQrcode;
        }

        var context = new EfiPixChargeContext { QrCodeImageUrl = qrImage };
        var charge = mappers.GetMapper<EfiPixCobResponse, Charge, EfiPixChargeContext>(context).Map(result.Data)!;
        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }

    public async Task<PaymentResponse<Charge>> GetAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var result = await client.GetChargeAsync(chargeId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, EfiErrorMapper.ToError(result), result.RawBody);
        }

        var charge = mappers.GetMapper<EfiPixCobResponse, Charge, EfiPixChargeContext>(new EfiPixChargeContext()).Map(result.Data)!;
        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }

    public Task<PaymentResponse<Charge>> CaptureAsync(string chargeId, Money? amount = null, CancellationToken cancellationToken = default)
        => Task.FromResult(PaymentResponse<Charge>.Fail(info, new PaymentError { Message = "PIX charges are not captured (they settle directly on payment)." }));

    public Task<PaymentResponse<Charge>> CancelAsync(string chargeId, CancellationToken cancellationToken = default)
        => Task.FromResult(PaymentResponse<Charge>.Fail(info, new PaymentError { Message = "Canceling a PIX charge is not supported by this client; let it expire instead." }));

    async Task<PaymentResponse<Refund>> IRefundOperations.CreateAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        // A Pix refund (devolucao) targets the end-to-end id of the settled payment, obtained from the charge.
        var chargeResult = await client.GetChargeAsync(request.ChargeId, cancellationToken).ConfigureAwait(false);
        if (!chargeResult.IsSuccess || chargeResult.Data is null)
        {
            return PaymentResponse<Refund>.Fail(info, EfiErrorMapper.ToError(chargeResult), chargeResult.RawBody);
        }

        var received = chargeResult.Data.Pix?.FirstOrDefault();
        if (received?.EndToEndId is not { } e2eId)
        {
            return PaymentResponse<Refund>.Fail(info, new PaymentError { Message = $"PIX charge '{request.ChargeId}' has no settled payment to refund." });
        }

        var amount = (request.Amount?.Amount ?? EfiPixChargeMapper.ParseAmount(received.Valor));
        var body = new EfiPixDevolucaoRequest { Valor = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) };
        var refundId = Guid.NewGuid().ToString("N");

        var result = await client.CreateRefundAsync(e2eId, refundId, body, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Refund>.Fail(info, EfiErrorMapper.ToError(result), result.RawBody);
        }

        var refund = mappers.GetMapper<EfiPixDevolucao, Refund>().Map(result.Data)!;
        return PaymentResponse<Refund>.Ok(info, new Refund
        {
            Id = refund.Id,
            ChargeId = request.ChargeId,
            Amount = refund.Amount,
            Status = refund.Status,
            ProviderStatus = refund.ProviderStatus,
            CreatedAt = refund.CreatedAt,
        }, result.RawBody);
    }
}
