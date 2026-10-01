using eQuantic.Mapper;
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Asaas.V3.Models;
using eQuantic.Payment.Http;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Asaas.V3;

/// <summary>Customer operations over the Asaas v3 <c>/customers</c> resource.</summary>
internal sealed class AsaasV3CustomerOperations(AsaasV3Client client, ProviderInfo info, IMapperFactory mappers)
    : ICustomerOperations
{
    public async Task<PaymentResponse<Customer>> CreateAsync(CustomerRequest request, CancellationToken cancellationToken = default)
    {
        var body = mappers.GetMapper<CustomerRequest, AsaasCustomerRequest>().Map(request)!;
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

    private PaymentResponse<Customer> MapCustomer(ApiResult<AsaasCustomerResponse> result)
    {
        if (!result.IsSuccess || result.Data is null)
        {
            return PaymentResponse<Customer>.Fail(info, AsaasErrorMapper.ToError(result), result.RawBody);
        }

        var customer = mappers.GetMapper<AsaasCustomerResponse, Customer>().Map(result.Data)!;
        return PaymentResponse<Customer>.Ok(info, customer, result.RawBody);
    }
}
