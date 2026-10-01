using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.PagSeguro.Orders.Models;

namespace eQuantic.Payment.PagSeguro.Orders;

/// <summary>Charge and refund operations over the PagSeguro Orders/Charges API.</summary>
internal sealed class PagSeguroOrdersOperations(PagSeguroOrdersClient client, ProviderInfo info, IMapperFactory mappers)
    : IChargeOperations, IRefundOperations
{
    public async Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default)
    {
        var body = mappers.GetMapper<CreateChargeRequest, PagSeguroOrderRequest>().Map(request)!;
        var result = await client.CreateOrderAsync(body, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, PagSeguroErrorMapper.ToError(result), result.RawBody);
        }

        var charge = mappers.GetMapper<PagSeguroOrderResponse, Charge>().Map(result.Data);
        return charge is null
            ? PaymentResponse<Charge>.Fail(info, new PaymentError { Message = "Order created without a charge or QR code.", HttpStatusCode = result.StatusCode }, result.RawBody)
            : PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }

    public async Task<PaymentResponse<Charge>> GetAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        // Card/boleto ids are charges (CHAR_); PIX charges are tracked by their order (ORDE_).
        if (chargeId.StartsWith("CHAR_", StringComparison.OrdinalIgnoreCase))
        {
            var chargeResult = await client.GetChargeAsync(chargeId, cancellationToken).ConfigureAwait(false);
            return MapCharge(chargeResult);
        }

        var orderResult = await client.GetOrderAsync(chargeId, cancellationToken).ConfigureAwait(false);
        if (!orderResult.IsSuccess || orderResult.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, PagSeguroErrorMapper.ToError(orderResult), orderResult.RawBody);
        }

        var charge = mappers.GetMapper<PagSeguroOrderResponse, Charge>().Map(orderResult.Data);
        return charge is null
            ? PaymentResponse<Charge>.Fail(info, new PaymentError { Message = $"Order '{chargeId}' has no charge or QR code.", HttpStatusCode = orderResult.StatusCode }, orderResult.RawBody)
            : PaymentResponse<Charge>.Ok(info, charge, orderResult.RawBody);
    }

    public Task<PaymentResponse<Charge>> CaptureAsync(string chargeId, Money? amount = null, CancellationToken cancellationToken = default)
        => CaptureAsync(new CaptureRequest { ChargeId = chargeId, Amount = amount }, cancellationToken);

    public async Task<PaymentResponse<Charge>> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default)
    {
        var result = await client.CaptureChargeAsync(request.ChargeId, request.Amount?.AmountInCents, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public Task<PaymentResponse<Charge>> CancelAsync(string chargeId, CancellationToken cancellationToken = default)
        => CancelAsync(new CancelRequest { ChargeId = chargeId }, cancellationToken);

    public async Task<PaymentResponse<Charge>> CancelAsync(CancelRequest request, CancellationToken cancellationToken = default)
    {
        var result = await client.CancelChargeAsync(request.ChargeId, amount: null, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    async Task<PaymentResponse<Refund>> IRefundOperations.CreateAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        var result = await client.CancelChargeAsync(request.ChargeId, request.Amount?.AmountInCents, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Refund>.Fail(info, PagSeguroErrorMapper.ToError(result), result.RawBody);
        }

        var refund = mappers.GetMapper<PagSeguroChargeResponse, Refund>().Map(result.Data)!;
        return PaymentResponse<Refund>.Ok(info, refund, result.RawBody);
    }

    private PaymentResponse<Charge> MapCharge(ApiResult<PagSeguroChargeResponse> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, PagSeguroErrorMapper.ToError(result), result.RawBody);
        }

        var charge = mappers.GetMapper<PagSeguroChargeResponse, Charge>().Map(result.Data)!;
        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }
}
