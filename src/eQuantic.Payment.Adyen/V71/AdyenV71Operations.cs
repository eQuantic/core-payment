using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Adyen.V71.Mapping;
using eQuantic.Payment.Adyen.V71.Models;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Adyen.V71;

/// <summary>Charge and refund operations over the Adyen Checkout API v71.</summary>
internal sealed class AdyenV71Operations(AdyenClientV71 client, ProviderInfo info, string merchantAccount, IMapperFactory mappers)
    : IChargeOperations, IRefundOperations
{
    public async Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default)
    {
        var body = mappers.GetMapper<CreateChargeRequest, AdyenPaymentRequest>().Map(request)!;
        body.MerchantAccount = merchantAccount;

        var result = await client.CreatePaymentAsync(body, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, AdyenErrorMapper.ToError(result), result.RawBody);
        }

        // A refusal is a valid HTTP 200 with resultCode "Refused"/"Error" — surface it as a failed response.
        if (AdyenStatusMapper.IsRefusal(result.Data.ResultCode))
        {
            return PaymentResponse<Charge>.Fail(info, new PaymentError
            {
                Code = result.Data.RefusalReasonCode,
                Message = result.Data.RefusalReason ?? $"Adyen refused the payment (resultCode '{result.Data.ResultCode}').",
                HttpStatusCode = result.StatusCode,
            }, result.RawBody);
        }

        var context = new AdyenChargeContext { Amount = request.Amount, Method = request.Method };
        var charge = mappers.GetMapper<AdyenPaymentResponse, Charge, AdyenChargeContext>(context).Map(result.Data)!;
        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }

    /// <summary>
    /// The Adyen Checkout API has no GET-by-id for payments. Poll <c>/payments/details</c> or rely on webhooks
    /// for the final status; this returns a failed response with an explanatory message.
    /// </summary>
    public Task<PaymentResponse<Charge>> GetAsync(string chargeId, CancellationToken cancellationToken = default)
        => Task.FromResult(PaymentResponse<Charge>.Fail(info, new PaymentError
        {
            Message = "Adyen Checkout has no GET payment-by-id; use the AUTHORISATION webhook or POST /payments/details for status.",
        }));

    public async Task<PaymentResponse<Charge>> CaptureAsync(string chargeId, Money? amount = null, CancellationToken cancellationToken = default)
    {
        // Adyen requires an amount to capture; it is included when supplied and otherwise omitted (Adyen then rejects it).
        var body = new AdyenCaptureRequest
        {
            MerchantAccount = merchantAccount,
            Amount = ToAmount(amount),
            Reference = chargeId,
        };

        var result = await client.CaptureAsync(chargeId, body, cancellationToken).ConfigureAwait(false);
        return MapModificationCharge(result);
    }

    public async Task<PaymentResponse<Charge>> CancelAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var body = new AdyenCancelRequest { MerchantAccount = merchantAccount, Reference = chargeId };
        var result = await client.CancelAsync(chargeId, body, cancellationToken).ConfigureAwait(false);
        return MapModificationCharge(result);
    }

    async Task<PaymentResponse<Refund>> IRefundOperations.CreateAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        // Adyen requires an amount to refund; it is included when supplied and otherwise omitted (Adyen then rejects it).
        var body = new AdyenRefundRequest
        {
            MerchantAccount = merchantAccount,
            Amount = ToAmount(request.Amount),
            Reference = request.ChargeId,
        };

        var result = await client.RefundAsync(request.ChargeId, body, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Refund>.Fail(info, AdyenErrorMapper.ToError(result), result.RawBody);
        }

        var refund = mappers.GetMapper<AdyenModificationResponse, Refund>().Map(result.Data)!;

        // The ack carries the original pspReference, but fall back to the request when absent.
        if (string.IsNullOrEmpty(refund.ChargeId) || refund.Amount is null)
        {
            refund = new Refund
            {
                Id = refund.Id,
                ChargeId = string.IsNullOrEmpty(refund.ChargeId) ? request.ChargeId : refund.ChargeId,
                Amount = refund.Amount ?? request.Amount,
                Status = refund.Status,
                ProviderStatus = refund.ProviderStatus,
                CreatedAt = refund.CreatedAt,
            };
        }

        return PaymentResponse<Refund>.Ok(info, refund, result.RawBody);
    }

    private static AdyenAmount? ToAmount(Money? amount)
        => amount is { } value ? new AdyenAmount { Value = value.AmountInCents, Currency = value.Currency } : null;

    private PaymentResponse<Charge> MapModificationCharge(ApiResult<AdyenModificationResponse> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, AdyenErrorMapper.ToError(result), result.RawBody);
        }

        var charge = mappers.GetMapper<AdyenModificationResponse, Charge>().Map(result.Data)!;
        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }
}
