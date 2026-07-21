using eQuantic.Payment.Models;

namespace eQuantic.Payment.Abstractions;

/// <summary>
/// Unified contract implemented by every payment provider adapter
/// (eQuantic.Payment.Pagarme, eQuantic.Payment.Stripe, ...).
/// </summary>
public interface IPaymentProvider
{
    /// <summary>Provider name and API version this instance talks to (e.g. <c>pagarme@v5</c>).</summary>
    ProviderInfo Info { get; }

    IChargeOperations Charges { get; }
    IRefundOperations Refunds { get; }
    ICustomerOperations Customers { get; }
}
