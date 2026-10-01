using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Abstractions;

/// <summary>
/// <see cref="IPaymentMethodOperations"/> for providers that do not save payment methods yet. Every call returns
/// a failed <see cref="PaymentResponse{T}"/> rather than throwing.
/// </summary>
public sealed class UnsupportedPaymentMethodOperations(ProviderInfo info) : IPaymentMethodOperations
{
    public Task<PaymentResponse<PaymentMethodSetup>> SetupAsync(PaymentMethodSetupRequest request, CancellationToken cancellationToken = default)
        => Task.FromResult(Unsupported<PaymentMethodSetup>());

    public Task<PaymentResponse<PaymentMethodSetup>> GetSetupAsync(string setupId, CancellationToken cancellationToken = default)
        => Task.FromResult(Unsupported<PaymentMethodSetup>());

    public Task<PaymentResponse<IReadOnlyList<SavedPaymentMethod>>> ListAsync(string customerId, CancellationToken cancellationToken = default)
        => Task.FromResult(Unsupported<IReadOnlyList<SavedPaymentMethod>>());

    public Task<PaymentResponse<SavedPaymentMethod>> DetachAsync(string paymentMethodId, CancellationToken cancellationToken = default)
        => Task.FromResult(Unsupported<SavedPaymentMethod>());

    private PaymentResponse<T> Unsupported<T>()
        => PaymentResponse<T>.Fail(info, new PaymentError
        {
            Code = "payment_methods_unsupported",
            Message = $"Provider '{info.Key}' does not save payment methods yet.",
        });
}
