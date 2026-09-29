using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Abstractions;

/// <summary>Unified operations on the notifications (webhooks) a gateway sends.</summary>
public interface INotificationOperations
{
    /// <summary>
    /// Verifies that a notification came from the gateway, then parses it. Nothing in a notification is to be
    /// trusted before this succeeds, and even then its object is best read again from the gateway before
    /// acting on it: gateways retry, and do not promise order.
    /// </summary>
    PaymentResponse<PaymentNotification> Verify(NotificationRequest request);
}
