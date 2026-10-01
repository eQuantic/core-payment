using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Abstractions;

/// <summary>
/// Unified operations on the payment methods a gateway keeps for a customer: a card saved once, then charged
/// with the customer away (<see cref="CreateChargeRequest.OffSession"/>).
/// </summary>
public interface IPaymentMethodOperations
{
    /// <summary>
    /// Starts saving a card for charges made with the customer away. The answer carries a client secret, which a
    /// front end confirms with the gateway's own SDK, so no card number passes through the caller.
    /// </summary>
    Task<PaymentResponse<PaymentMethodSetup>> SetupAsync(PaymentMethodSetupRequest request, CancellationToken cancellationToken = default);

    /// <summary>Reads a setup again: once it succeeds, it names the payment method saved.</summary>
    Task<PaymentResponse<PaymentMethodSetup>> GetSetupAsync(string setupId, CancellationToken cancellationToken = default);

    /// <summary>The cards saved for a customer.</summary>
    Task<PaymentResponse<IReadOnlyList<SavedPaymentMethod>>> ListAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>Detaches a saved payment method from its customer: it can no longer be charged.</summary>
    Task<PaymentResponse<SavedPaymentMethod>> DetachAsync(string paymentMethodId, CancellationToken cancellationToken = default);
}
