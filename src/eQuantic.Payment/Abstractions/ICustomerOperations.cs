using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Abstractions;

/// <summary>Unified customer operations.</summary>
public interface ICustomerOperations
{
    Task<PaymentResponse<Customer>> CreateAsync(CustomerRequest request, CancellationToken cancellationToken = default);

    Task<PaymentResponse<Customer>> GetAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a customer's name, email, phone, document and address with <paramref name="request"/>'s, so the
    /// gateway's copy stays in step with yours. A provider that does not update customers answers with a failure
    /// (<c>customer_update_unsupported</c>); one built against 1.x that does not implement this throws
    /// <see cref="NotSupportedException"/>.
    /// </summary>
    Task<PaymentResponse<Customer>> UpdateAsync(string customerId, CustomerRequest request, CancellationToken cancellationToken = default)
        => throw new NotSupportedException($"{GetType().Name} does not update customers.");
}
