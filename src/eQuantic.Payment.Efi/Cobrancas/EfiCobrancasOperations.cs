using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Efi.Cobrancas.Models;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Efi.Cobrancas;

/// <summary>Charge and refund operations over the Efí Cobranças API (boleto and credit card).</summary>
internal sealed class EfiCobrancasOperations(EfiCobrancasClient client, ProviderInfo info, IMapperFactory mappers)
    : IChargeOperations, IRefundOperations
{
    public async Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Method == PaymentMethodType.Pix)
        {
            return PaymentResponse<Charge>.Fail(info, new PaymentError
            {
                Message = "The Efí Cobranças API handles boleto and credit card. Register the 'pix' version for PIX.",
            });
        }

        var body = mappers.GetMapper<CreateChargeRequest, EfiOneStepRequest>().Map(request)!;
        var result = await client.CreateOneStepAsync(body, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public async Task<PaymentResponse<Charge>> GetAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var result = await client.GetChargeAsync(chargeId, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public Task<PaymentResponse<Charge>> CaptureAsync(string chargeId, Money? amount = null, CancellationToken cancellationToken = default)
        => Task.FromResult(PaymentResponse<Charge>.Fail(info, new PaymentError { Message = "Efí Cobranças one-step charges are captured automatically; separate capture is not supported." }));

    public async Task<PaymentResponse<Charge>> CancelAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var result = await client.CancelChargeAsync(chargeId, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    async Task<PaymentResponse<Refund>> IRefundOperations.CreateAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        // Efí supports online refund for credit card (estorno); boleto has no online refund.
        var result = await client.RefundChargeAsync(request.ChargeId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data?.Data is null)
        {
            return PaymentResponse<Refund>.Fail(info, EfiErrorMapper.ToError(result), result.RawBody);
        }

        var refund = mappers.GetMapper<EfiChargeData, Refund>().Map(result.Data.Data)!;
        return PaymentResponse<Refund>.Ok(info, refund, result.RawBody);
    }

    private PaymentResponse<Charge> MapCharge(ApiResult<EfiOneStepResponse> result)
    {
        if (!result.IsSuccess || result.Data?.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, EfiErrorMapper.ToError(result), result.RawBody);
        }

        var charge = mappers.GetMapper<EfiChargeData, Charge>().Map(result.Data.Data)!;
        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }
}
