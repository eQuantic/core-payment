using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Http;
using eQuantic.Payment.MercadoPago.Payments.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.MercadoPago.Payments;

/// <summary>Charge and refund operations over the Mercado Pago Payments API (<c>/v1/payments</c>).</summary>
internal sealed class MercadoPagoPaymentsOperations(MercadoPagoPaymentsClient client, ProviderInfo info, IMapperFactory mappers)
    : IChargeOperations, IRefundOperations
{
    public async Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default)
    {
        var body = mappers.GetMapper<CreateChargeRequest, MercadoPagoPaymentRequest>().Map(request)!;
        var result = await client.CreatePaymentAsync(body, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public async Task<PaymentResponse<Charge>> GetAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(chargeId, out var id))
        {
            return PaymentResponse<Charge>.Fail(info, new PaymentError { Message = $"Invalid Mercado Pago payment id '{chargeId}'." });
        }

        var result = await client.GetPaymentAsync(id, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public Task<PaymentResponse<Charge>> CaptureAsync(string chargeId, Money? amount = null, CancellationToken cancellationToken = default)
        => CaptureAsync(new CaptureRequest { ChargeId = chargeId, Amount = amount }, cancellationToken);

    public async Task<PaymentResponse<Charge>> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(request.ChargeId, out var id))
        {
            return PaymentResponse<Charge>.Fail(info, new PaymentError { Message = $"Invalid Mercado Pago payment id '{request.ChargeId}'." });
        }

        var body = new MercadoPagoCaptureRequest { Capture = true, TransactionAmount = request.Amount?.Amount };
        var result = await client.CapturePaymentAsync(id, body, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public Task<PaymentResponse<Charge>> CancelAsync(string chargeId, CancellationToken cancellationToken = default)
        => CancelAsync(new CancelRequest { ChargeId = chargeId }, cancellationToken);

    public async Task<PaymentResponse<Charge>> CancelAsync(CancelRequest request, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(request.ChargeId, out var id))
        {
            return PaymentResponse<Charge>.Fail(info, new PaymentError { Message = $"Invalid Mercado Pago payment id '{request.ChargeId}'." });
        }

        var result = await client.CancelPaymentAsync(id, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    async Task<PaymentResponse<Refund>> IRefundOperations.CreateAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        if (!long.TryParse(request.ChargeId, out var id))
        {
            return PaymentResponse<Refund>.Fail(info, new PaymentError { Message = $"Invalid Mercado Pago payment id '{request.ChargeId}'." });
        }

        var result = await client.CreateRefundAsync(id, new MercadoPagoRefundRequest { Amount = request.Amount?.Amount }, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Refund>.Fail(info, MercadoPagoErrorMapper.ToError(result), result.RawBody);
        }

        var refund = mappers.GetMapper<MercadoPagoRefundResponse, Refund>().Map(result.Data)!;
        return PaymentResponse<Refund>.Ok(info, refund, result.RawBody);
    }

    private PaymentResponse<Charge> MapCharge(ApiResult<MercadoPagoPaymentResponse> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, MercadoPagoErrorMapper.ToError(result), result.RawBody);
        }

        var charge = mappers.GetMapper<MercadoPagoPaymentResponse, Charge>().Map(result.Data)!;
        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }
}
