using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Http;
using eQuantic.Payment.MercadoPago.Customers.Models;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.MercadoPago.Customers;

/// <summary>Shared customer operations over <c>/v1/customers</c>, used by both API versions.</summary>
internal sealed class MercadoPagoCustomerOperations(MercadoPagoCustomerClient client, ProviderInfo info, IMapperFactory mappers)
    : ICustomerOperations
{
    public async Task<PaymentResponse<Customer>> CreateAsync(CustomerRequest request, CancellationToken cancellationToken = default)
    {
        var body = mappers.GetMapper<CustomerRequest, MercadoPagoCustomerRequest>().Map(request)!;
        var result = await client.CreateCustomerAsync(body, cancellationToken).ConfigureAwait(false);
        return MapCustomer(result);
    }

    public async Task<PaymentResponse<Customer>> GetAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var result = await client.GetCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        return MapCustomer(result);
    }

    public Task<PaymentResponse<Customer>> UpdateAsync(string customerId, CustomerRequest request, CancellationToken cancellationToken = default)
        => CustomerUpdates.Unsupported(info);

    private PaymentResponse<Customer> MapCustomer(ApiResult<MercadoPagoCustomerResponse> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Customer>.Fail(info, MercadoPagoErrorMapper.ToError(result), result.RawBody);
        }

        var customer = mappers.GetMapper<MercadoPagoCustomerResponse, Customer>().Map(result.Data)!;
        return PaymentResponse<Customer>.Ok(info, customer, result.RawBody);
    }
}
