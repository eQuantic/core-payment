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

    /// <summary>
    /// Verifies and parses the notifications (webhooks) the gateway sends. A provider that does not verify
    /// them yet answers every call with a failure.
    /// </summary>
    INotificationOperations Notifications => new UnsupportedNotificationOperations(Info);

    /// <summary>
    /// Saves a customer's card for charges made with the customer away, and lists and detaches the cards saved.
    /// A provider that does not save payment methods yet answers every call with a failure.
    /// </summary>
    IPaymentMethodOperations PaymentMethods => new UnsupportedPaymentMethodOperations(Info);
}
