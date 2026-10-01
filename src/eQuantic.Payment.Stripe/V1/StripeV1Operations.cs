using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Stripe.V1.Mapping;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1;

/// <summary>Stripe exposes charges, refunds and customers over one REST client, so one class serves all three.</summary>
internal sealed class StripeV1Operations(StripeClientV1 client, ProviderInfo info, IMapperFactory mappers)
    : IChargeOperations, IRefundOperations, ICustomerOperations
{
    public async Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default)
    {
        var context = new StripeRequestContext { Today = DateOnly.FromDateTime((request.AttemptedAt ?? DateTimeOffset.UtcNow).UtcDateTime) };
        var form = mappers.GetMapper<CreateChargeRequest, StripeForm, StripeRequestContext>(context).Map(request)!;
        var result = await client.CreatePaymentIntentAsync(form, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public async Task<PaymentResponse<Charge>> GetAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var result = await client.GetPaymentIntentAsync(chargeId, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public Task<PaymentResponse<Charge>> CaptureAsync(string chargeId, Money? amount = null, CancellationToken cancellationToken = default)
        => CaptureAsync(new CaptureRequest { ChargeId = chargeId, Amount = amount }, cancellationToken);

    public async Task<PaymentResponse<Charge>> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default)
    {
        var form = request.Amount is { } a ? [new KeyValuePair<string, string>("amount_to_capture", a.AmountInCents.ToString())] : (IEnumerable<KeyValuePair<string, string>>?)null;
        var result = await client.CapturePaymentIntentAsync(request.ChargeId, form, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public Task<PaymentResponse<Charge>> CancelAsync(string chargeId, CancellationToken cancellationToken = default)
        => CancelAsync(new CancelRequest { ChargeId = chargeId }, cancellationToken);

    public async Task<PaymentResponse<Charge>> CancelAsync(CancelRequest request, CancellationToken cancellationToken = default)
    {
        var result = await client.CancelPaymentIntentAsync(request.ChargeId, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    async Task<PaymentResponse<Refund>> IRefundOperations.CreateAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        var form = mappers.GetMapper<RefundRequest, StripeForm>().Map(request)!;
        var result = await client.CreateRefundAsync(form, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Refund>.Fail(info, StripeErrorMapper.ToError(result), result.RawBody);
        }

        var refund = mappers.GetMapper<StripeRefund, Refund>().Map(result.Data)!;
        if (string.IsNullOrEmpty(refund.ChargeId))
        {
            refund = new Refund
            {
                Id = refund.Id,
                ChargeId = request.ChargeId,
                Amount = refund.Amount,
                Status = refund.Status,
                ProviderStatus = refund.ProviderStatus,
                CreatedAt = refund.CreatedAt,
            };
        }

        return PaymentResponse<Refund>.Ok(info, refund, result.RawBody);
    }

    async Task<PaymentResponse<Customer>> ICustomerOperations.CreateAsync(CustomerRequest request, CancellationToken cancellationToken)
    {
        var form = mappers.GetMapper<CustomerRequest, StripeForm>().Map(request)!;
        var result = await client.CreateCustomerAsync(form, cancellationToken).ConfigureAwait(false);
        return MapCustomer(result);
    }

    async Task<PaymentResponse<Customer>> ICustomerOperations.GetAsync(string customerId, CancellationToken cancellationToken)
    {
        var result = await client.GetCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        return MapCustomer(result);
    }

    private PaymentResponse<Charge> MapCharge(ApiResult<StripePaymentIntent> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, StripeErrorMapper.ToError(result), result.RawBody);
        }

        var charge = mappers.GetMapper<StripePaymentIntent, Charge>().Map(result.Data)!;
        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }

    private PaymentResponse<Customer> MapCustomer(ApiResult<StripeCustomer> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Customer>.Fail(info, StripeErrorMapper.ToError(result), result.RawBody);
        }

        var customer = mappers.GetMapper<StripeCustomer, Customer>().Map(result.Data)!;
        return PaymentResponse<Customer>.Ok(info, customer, result.RawBody);
    }
}
