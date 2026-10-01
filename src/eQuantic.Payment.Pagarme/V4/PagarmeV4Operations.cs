using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Pagarme.V4.Models;

namespace eQuantic.Payment.Pagarme.V4;

/// <summary>
/// V4 exposes charges, refunds and customers over a single <c>transactions</c> resource,
/// so one class implements all three operation contracts.
/// </summary>
internal sealed class PagarmeV4Operations(PagarmeClientV4 client, ProviderInfo info, IMapperFactory mappers)
    : IChargeOperations, IRefundOperations, ICustomerOperations
{
    public async Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default)
    {
        var body = mappers.GetMapper<CreateChargeRequest, V4TransactionRequest>().Map(request)!;
        var result = await client.CreateTransactionAsync(body, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public async Task<PaymentResponse<Charge>> GetAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(chargeId, out var id))
        {
            return PaymentResponse<Charge>.Fail(info, new PaymentError { Message = $"Invalid v4 transaction id '{chargeId}'." });
        }

        var result = await client.GetTransactionAsync(id, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public async Task<PaymentResponse<Charge>> CaptureAsync(string chargeId, Money? amount = null, CancellationToken cancellationToken = default)
    {
        if (!long.TryParse(chargeId, out var id))
        {
            return PaymentResponse<Charge>.Fail(info, new PaymentError { Message = $"Invalid v4 transaction id '{chargeId}'." });
        }

        var result = await client.CaptureTransactionAsync(id, amount?.AmountInCents, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public async Task<PaymentResponse<Charge>> CancelAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        // v4 has no "cancel"; a full refund voids an unsettled transaction.
        if (!long.TryParse(chargeId, out var id))
        {
            return PaymentResponse<Charge>.Fail(info, new PaymentError { Message = $"Invalid v4 transaction id '{chargeId}'." });
        }

        var result = await client.RefundTransactionAsync(id, amount: null, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    async Task<PaymentResponse<Refund>> IRefundOperations.CreateAsync(RefundRequest request, CancellationToken cancellationToken)
    {
        if (!long.TryParse(request.ChargeId, out var id))
        {
            return PaymentResponse<Refund>.Fail(info, new PaymentError { Message = $"Invalid v4 transaction id '{request.ChargeId}'." });
        }

        var result = await client.RefundTransactionAsync(id, request.Amount?.AmountInCents, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Refund>.Fail(info, PagarmeErrorMapper.ToError(result, "v4"), result.RawBody);
        }

        var refund = mappers.GetMapper<V4TransactionResponse, Refund>().Map(result.Data)!;
        if (request.Amount is { } amount)
        {
            refund = new Refund
            {
                Id = refund.Id,
                ChargeId = refund.ChargeId,
                Amount = amount,
                Status = refund.Status,
                ProviderStatus = refund.ProviderStatus,
                CreatedAt = refund.CreatedAt,
            };
        }

        return PaymentResponse<Refund>.Ok(info, refund, result.RawBody);
    }

    async Task<PaymentResponse<Customer>> ICustomerOperations.CreateAsync(CustomerRequest request, CancellationToken cancellationToken)
    {
        var body = mappers.GetMapper<CustomerRequest, V4CustomerRequest>().Map(request)!;
        var result = await client.CreateCustomerAsync(body, cancellationToken).ConfigureAwait(false);
        return MapCustomer(result);
    }

    async Task<PaymentResponse<Customer>> ICustomerOperations.GetAsync(string customerId, CancellationToken cancellationToken)
    {
        if (!long.TryParse(customerId, out var id))
        {
            return PaymentResponse<Customer>.Fail(info, new PaymentError { Message = $"Invalid v4 customer id '{customerId}'." });
        }

        var result = await client.GetCustomerAsync(id, cancellationToken).ConfigureAwait(false);
        return MapCustomer(result);
    }

    Task<PaymentResponse<Customer>> ICustomerOperations.UpdateAsync(string customerId, CustomerRequest request, CancellationToken cancellationToken)
        => CustomerUpdates.Unsupported(info);

    private PaymentResponse<Charge> MapCharge(ApiResult<V4TransactionResponse> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, PagarmeErrorMapper.ToError(result, "v4"), result.RawBody);
        }

        var charge = mappers.GetMapper<V4TransactionResponse, Charge>().Map(result.Data)!;
        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }

    private PaymentResponse<Customer> MapCustomer(ApiResult<V4CustomerResponse> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Customer>.Fail(info, PagarmeErrorMapper.ToError(result, "v4"), result.RawBody);
        }

        var customer = mappers.GetMapper<V4CustomerResponse, Customer>().Map(result.Data)!;
        return PaymentResponse<Customer>.Ok(info, customer, result.RawBody);
    }
}
