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
}
