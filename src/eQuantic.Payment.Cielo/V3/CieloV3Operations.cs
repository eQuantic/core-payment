using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Cielo.V3.Models;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Cielo.V3;

/// <summary>Charge and refund operations over the Cielo API 3.0 sales resource.</summary>
internal sealed class CieloV3Operations(CieloClient client, ProviderInfo info, IMapperFactory mappers)
    : IChargeOperations, IRefundOperations
{
    public async Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default)
    {
        var body = mappers.GetMapper<CreateChargeRequest, CieloSaleRequest>().Map(request)!;
        var result = await client.CreateSaleAsync(body, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, CieloErrorMapper.ToError(result), result.RawBody);
        }

        var charge = mappers.GetMapper<CieloSaleResponse, Charge>().Map(result.Data);
        return charge is null
            ? PaymentResponse<Charge>.Fail(info, new PaymentError { Message = "Sale created without payment data.", HttpStatusCode = result.StatusCode }, result.RawBody)
            : PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }

    public async Task<PaymentResponse<Charge>> GetAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var result = await client.GetSaleAsync(chargeId, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, CieloErrorMapper.ToError(result), result.RawBody);
        }

        var charge = mappers.GetMapper<CieloSaleResponse, Charge>().Map(result.Data);
        return charge is null
            ? PaymentResponse<Charge>.Fail(info, new PaymentError { Message = $"Sale '{chargeId}' has no payment data.", HttpStatusCode = result.StatusCode }, result.RawBody)
            : PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }

    public async Task<PaymentResponse<Charge>> CaptureAsync(string chargeId, Money? amount = null, CancellationToken cancellationToken = default)
    {
        var result = await client.CaptureSaleAsync(chargeId, amount?.AmountInCents, cancellationToken).ConfigureAwait(false);
        return MapReturn(result, chargeId, amount);
    }

    public async Task<PaymentResponse<Charge>> CancelAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var result = await client.VoidSaleAsync(chargeId, amount: null, cancellationToken).ConfigureAwait(false);
        return MapReturn(result, chargeId, amount: null);
    }

    async Task<PaymentResponse<Refund>> IRefundOperations.CreateAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        var result = await client.VoidSaleAsync(request.ChargeId, request.Amount?.AmountInCents, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Refund>.Fail(info, CieloErrorMapper.ToError(result), result.RawBody);
        }

        var mapped = mappers.GetMapper<CieloReturnResponse, Refund>().Map(result.Data)!;
        var refund = new Refund
        {
            Id = request.ChargeId,
            ChargeId = request.ChargeId,
            Amount = request.Amount,
            Status = mapped.Status,
            ProviderStatus = mapped.ProviderStatus,
            CreatedAt = mapped.CreatedAt,
        };

        return PaymentResponse<Refund>.Ok(info, refund, result.RawBody);
    }

    private PaymentResponse<Charge> MapReturn(ApiResult<CieloReturnResponse> result, string chargeId, Money? amount)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, CieloErrorMapper.ToError(result), result.RawBody);
        }

        var mapped = mappers.GetMapper<CieloReturnResponse, Charge>().Map(result.Data)!;
        var charge = new Charge
        {
            Id = chargeId,
            Status = mapped.Status,
            ProviderStatus = mapped.ProviderStatus,
            Amount = amount ?? mapped.Amount,
            Method = mapped.Method,
            Card = mapped.Card,
        };

        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }
}
