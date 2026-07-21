using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Http;
using eQuantic.Payment.MercadoPago.Orders.Mapping;
using eQuantic.Payment.MercadoPago.Orders.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.MercadoPago.Orders;

/// <summary>Charge and refund operations over the Mercado Pago Orders API (<c>/v1/orders</c>).</summary>
internal sealed class MercadoPagoOrdersOperations(MercadoPagoOrdersClient client, ProviderInfo info, IMapperFactory mappers)
    : IChargeOperations, IRefundOperations
{
    public async Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default)
    {
        var body = mappers.GetMapper<CreateChargeRequest, MercadoPagoOrderRequest>().Map(request)!;
        var result = await client.CreateOrderAsync(body, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public async Task<PaymentResponse<Charge>> GetAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var result = await client.GetOrderAsync(chargeId, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public async Task<PaymentResponse<Charge>> CaptureAsync(string chargeId, Money? amount = null, CancellationToken cancellationToken = default)
    {
        var result = await client.CaptureOrderAsync(chargeId, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public async Task<PaymentResponse<Charge>> CancelAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var result = await client.CancelOrderAsync(chargeId, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    async Task<PaymentResponse<Refund>> IRefundOperations.CreateAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        var body = new MercadoPagoOrderRefundRequest { Amount = request.Amount is { } a ? MercadoPagoOrderWire.ToAmount(a.Amount) : null };
        var result = await client.RefundOrderAsync(request.ChargeId, body, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Refund>.Fail(info, MercadoPagoErrorMapper.ToError(result), result.RawBody);
        }

        var refund = mappers.GetMapper<MercadoPagoOrderResponse, Refund>().Map(result.Data)!;
        return PaymentResponse<Refund>.Ok(info, refund, result.RawBody);
    }

    private PaymentResponse<Charge> MapCharge(ApiResult<MercadoPagoOrderResponse> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, MercadoPagoErrorMapper.ToError(result), result.RawBody);
        }

        var charge = mappers.GetMapper<MercadoPagoOrderResponse, Charge>().Map(result.Data)!;
        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }
}
