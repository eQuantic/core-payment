using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Abstractions;

/// <summary>Unified refund operations.</summary>
public interface IRefundOperations
{
    Task<PaymentResponse<Refund>> CreateAsync(RefundRequest request, CancellationToken cancellationToken = default);
}
