using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Abstractions;

/// <summary>Unified charge operations (PIX, boleto, cards).</summary>
public interface IChargeOperations
{
    Task<PaymentResponse<Charge>> CreateAsync(CreateChargeRequest request, CancellationToken cancellationToken = default);

    Task<PaymentResponse<Charge>> GetAsync(string chargeId, CancellationToken cancellationToken = default);

    /// <summary>Captures a previously authorized card charge. Pass <paramref name="amount"/> for partial capture.</summary>
    Task<PaymentResponse<Charge>> CaptureAsync(string chargeId, Money? amount = null, CancellationToken cancellationToken = default);

    Task<PaymentResponse<Charge>> CancelAsync(string chargeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures a previously authorized card charge, under the caller's idempotency key. A provider whose gateway
    /// takes no key captures as <see cref="CaptureAsync(string, Money?, CancellationToken)"/> does.
    /// </summary>
    Task<PaymentResponse<Charge>> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default)
        => CaptureAsync(request.ChargeId, request.Amount, cancellationToken);

    /// <summary>
    /// Cancels a charge not yet settled, under the caller's idempotency key. A provider whose gateway takes no key
    /// cancels as <see cref="CancelAsync(string, CancellationToken)"/> does.
    /// </summary>
    Task<PaymentResponse<Charge>> CancelAsync(CancelRequest request, CancellationToken cancellationToken = default)
        => CancelAsync(request.ChargeId, cancellationToken);
}
