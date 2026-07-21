using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Pagarme.V5.Models;

namespace eQuantic.Payment.Pagarme.V5;

internal sealed class PagarmeV5ChargeOperations(PagarmeClientV5 client, ProviderInfo info, IMapperFactory mappers) : IChargeOperations
{
    public async Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default)
    {
        var order = mappers.GetMapper<CreateChargeRequest, V5OrderRequest>().Map(request)!;
        var result = await client.CreateOrderAsync(order, cancellationToken).ConfigureAwait(false);
        return MapOrder(result);
    }

    public async Task<PaymentResponse<Charge>> GetAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var result = await client.GetChargeAsync(chargeId, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public async Task<PaymentResponse<Charge>> CaptureAsync(string chargeId, Money? amount = null, CancellationToken cancellationToken = default)
    {
        var result = await client.CaptureChargeAsync(chargeId, amount?.AmountInCents, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    public async Task<PaymentResponse<Charge>> CancelAsync(string chargeId, CancellationToken cancellationToken = default)
    {
        var result = await client.CancelChargeAsync(chargeId, amount: null, cancellationToken).ConfigureAwait(false);
        return MapCharge(result);
    }

    private PaymentResponse<Charge> MapOrder(ApiResult<V5OrderResponse> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, PagarmeErrorMapper.ToError(result, "v5"), result.RawBody);
        }

        var charge = mappers.GetMapper<V5OrderResponse, Charge>().Map(result.Data);
        return charge is null
            ? PaymentResponse<Charge>.Fail(info, new PaymentError { Message = "Order created without charges.", HttpStatusCode = result.StatusCode }, result.RawBody)
            : PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }

    private PaymentResponse<Charge> MapCharge(ApiResult<V5ChargeResponse> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Charge>.Fail(info, PagarmeErrorMapper.ToError(result, "v5"), result.RawBody);
        }

        var charge = mappers.GetMapper<V5ChargeResponse, Charge>().Map(result.Data)!;
        return PaymentResponse<Charge>.Ok(info, charge, result.RawBody);
    }
}

internal sealed class PagarmeV5RefundOperations(PagarmeClientV5 client, ProviderInfo info, IMapperFactory mappers) : IRefundOperations
{
    public async Task<PaymentResponse<Refund>> CreateAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        var result = await client.CancelChargeAsync(request.ChargeId, request.Amount?.AmountInCents, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Refund>.Fail(info, PagarmeErrorMapper.ToError(result, "v5"), result.RawBody);
        }

        var refund = mappers.GetMapper<V5ChargeResponse, Refund>().Map(result.Data)!;
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
}

internal sealed class PagarmeV5CustomerOperations(PagarmeClientV5 client, ProviderInfo info, IMapperFactory mappers) : ICustomerOperations
{
    public async Task<PaymentResponse<Customer>> CreateAsync(CustomerRequest request, CancellationToken cancellationToken = default)
    {
        var body = mappers.GetMapper<CustomerRequest, V5CustomerRequest>().Map(request)!;
        var result = await client.CreateCustomerAsync(body, cancellationToken).ConfigureAwait(false);
        return MapCustomer(result);
    }

    public async Task<PaymentResponse<Customer>> GetAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var result = await client.GetCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        return MapCustomer(result);
    }

    private PaymentResponse<Customer> MapCustomer(ApiResult<V5CustomerResponse> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Customer>.Fail(info, PagarmeErrorMapper.ToError(result, "v5"), result.RawBody);
        }

        var customer = mappers.GetMapper<V5CustomerResponse, Customer>().Map(result.Data)!;
        return PaymentResponse<Customer>.Ok(info, customer, result.RawBody);
    }
}
