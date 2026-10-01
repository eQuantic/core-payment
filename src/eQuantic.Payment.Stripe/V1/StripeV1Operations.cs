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
internal sealed class StripeV1Operations(StripeClientV1 client, ProviderInfo info, IMapperFactory mappers, TimeProvider clock)
    : IChargeOperations, IRefundOperations, ICustomerOperations
{
    public async Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default)
    {
        if (Refuse(request) is { } refusal)
        {
            return PaymentResponse<Charge>.Fail(info, refusal);
        }

        // São Paulo's day of the attempt: Stripe counts a boleto's days there, and a retry counts from the first try.
        var context = new StripeRequestContext { Today = StripeBoleto.Today(request.AttemptedAt ?? clock.GetUtcNow()) };
        var form = mappers.GetMapper<CreateChargeRequest, StripeForm, StripeRequestContext>(context).Map(request)!;
        var result = await client.CreatePaymentIntentAsync(form, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    /// <summary>What Stripe would refuse anyway, refused before the call with a reason the caller can act on.</summary>
    private static PaymentError? Refuse(CreateChargeRequest request)
    {
        if (request.OffSession
            && (string.IsNullOrWhiteSpace(request.CustomerId)
                || request.Method is not (PaymentMethodType.CreditCard or PaymentMethodType.DebitCard)
                || string.IsNullOrWhiteSpace(StripePaymentIntentFormMapper.SavedPaymentMethod(request.Card))))
        {
            return new PaymentError
            {
                Code = "saved_payment_method_required",
                Message = "A charge with the customer away needs the customer's id and a card saved for it (CardDetails.PaymentMethodId).",
            };
        }

        if (request.Method == PaymentMethodType.Boleto && !IsWholePayer(request.Customer))
        {
            return new PaymentError
            {
                Code = "boleto_payer_incomplete",
                Message = "A boleto needs the payer's name, email, CPF or CNPJ and full address (CreateChargeRequest.Customer).",
            };
        }

        return null;
    }

    /// <summary>A boleto's payer: name, email, CPF or CNPJ and full address, none of them blank.</summary>
    private static bool IsWholePayer(CustomerRequest? payer) =>
        payer is { DocumentType: not null, Address: { } address }
        && !string.IsNullOrWhiteSpace(payer.Name)
        && !string.IsNullOrWhiteSpace(payer.Email)
        && !string.IsNullOrWhiteSpace(address.Line1)
        && !string.IsNullOrWhiteSpace(address.City)
        && !string.IsNullOrWhiteSpace(address.State)
        && !string.IsNullOrWhiteSpace(address.ZipCode)
        && !string.IsNullOrWhiteSpace(address.Country);

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
        var result = await client.CreateCustomerAsync(form, request.IdempotencyKey, cancellationToken).ConfigureAwait(false);
        return MapCustomer(result);
    }

    async Task<PaymentResponse<Customer>> ICustomerOperations.GetAsync(string customerId, CancellationToken cancellationToken)
    {
        var result = await client.GetCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        return MapCustomer(result);
    }

    async Task<PaymentResponse<Customer>> ICustomerOperations.UpdateAsync(string customerId, CustomerRequest request, CancellationToken cancellationToken)
    {
        var form = mappers.GetMapper<CustomerRequest, StripeForm>().Map(request)!;
        StripeCustomerFormMapper.ClearOmitted(form, request);
        var result = await client.UpdateCustomerAsync(customerId, form, cancellationToken).ConfigureAwait(false);
        return MapCustomer(result);
    }

    private PaymentResponse<Charge> MapCharge(ApiResult<StripePaymentIntent> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            // A declined card, or one that asks for authentication with the customer away, is a 402 whose error
            // still carries the PaymentIntent: the failure comes back with the charge, its id and its status.
            var error = StripeErrorMapper.Parse(result);
            var declined = error?.PaymentIntent is { } intent ? mappers.GetMapper<StripePaymentIntent, Charge>().Map(intent) : null;
            return PaymentResponse<Charge>.Fail(info, StripeErrorMapper.ToError(error, result.StatusCode), declined, result.RawBody);
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
