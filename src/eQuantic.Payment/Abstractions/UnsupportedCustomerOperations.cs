using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Abstractions;

/// <summary>
/// <see cref="ICustomerOperations"/> for providers that expose no standalone customer resource
/// (the customer is sent inline with each charge). Every call returns a failed
/// <see cref="PaymentResponse{T}"/> with an explanatory message rather than throwing.
/// </summary>
public sealed class UnsupportedCustomerOperations(ProviderInfo info) : ICustomerOperations
{
    public Task<PaymentResponse<Customer>> CreateAsync(CustomerRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(Unsupported());

    public Task<PaymentResponse<Customer>> GetAsync(string customerId, CancellationToken cancellationToken = default)
        => Task.FromResult(Unsupported());

    public Task<PaymentResponse<Customer>> UpdateAsync(string customerId, CustomerRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(Unsupported());

    private PaymentResponse<Customer> Unsupported()
        => PaymentResponse<Customer>.Fail(info, new PaymentError
        {
            Message = $"Provider '{info.Key}' has no standalone customer resource; send customer data inline with the charge.",
        });
}
