using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Abstractions;

/// <summary>
/// <see cref="INotificationOperations"/> for providers whose notifications this package does not verify yet.
/// Every call returns a failed <see cref="PaymentResponse{T}"/> rather than throwing, so no notification is
/// taken for verified by mistake.
/// </summary>
public sealed class UnsupportedNotificationOperations(ProviderInfo info) : INotificationOperations
{
    public PaymentResponse<PaymentNotification> Verify(NotificationRequest request)
        => PaymentResponse<PaymentNotification>.Fail(info, new PaymentError
        {
            Code = "notifications_unsupported",
            Message = $"Provider '{info.Key}' does not verify its notifications yet.",
        });
}
