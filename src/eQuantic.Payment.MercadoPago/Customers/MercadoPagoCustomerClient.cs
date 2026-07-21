using eQuantic.Payment.Http;
using eQuantic.Payment.MercadoPago.Customers.Models;

namespace eQuantic.Payment.MercadoPago.Customers;

/// <summary>Typed client for the shared Mercado Pago customers resource (<c>/v1/customers</c>).</summary>
public class MercadoPagoCustomerClient(HttpClient httpClient) : MercadoPagoClientBase(httpClient)
{
    public Task<ApiResult<MercadoPagoCustomerResponse>> CreateCustomerAsync(MercadoPagoCustomerRequest request, CancellationToken cancellationToken = default)
        => SendJsonAsync<MercadoPagoCustomerResponse>(HttpMethod.Post, "v1/customers", request, cancellationToken);

    public Task<ApiResult<MercadoPagoCustomerResponse>> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default)
        => SendJsonAsync<MercadoPagoCustomerResponse>(HttpMethod.Get, $"v1/customers/{customerId}", null, cancellationToken);
}
