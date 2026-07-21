using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Abstractions;

/// <summary>Unified customer operations.</summary>
public interface ICustomerOperations
{
    Task<PaymentResponse<Customer>> CreateAsync(CustomerRequest request, CancellationToken cancellationToken = default);

    Task<PaymentResponse<Customer>> GetAsync(string customerId, CancellationToken cancellationToken = default);
}
