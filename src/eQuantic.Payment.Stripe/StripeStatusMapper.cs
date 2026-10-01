using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Stripe;

internal static class StripeStatusMapper
{
    /// <summary>Maps a PaymentIntent status to the unified enum.</summary>
    public static PaymentStatus FromPaymentIntent(string? status) => status?.ToLowerInvariant() switch
    {
        "requires_payment_method" => PaymentStatus.Pending,
        "requires_confirmation" => PaymentStatus.Pending,
        "requires_action" => PaymentStatus.Pending,
        "processing" => PaymentStatus.Processing,
        "requires_capture" => PaymentStatus.Authorized,
        "succeeded" => PaymentStatus.Paid,
        "canceled" => PaymentStatus.Canceled,
        _ => PaymentStatus.Unknown,
    };

    /// <summary>
    /// Maps a PaymentIntent to the unified enum, reading its last error too: one waiting for a payment method
    /// after an attempt failed did not merely not start. Its attempt expired (an unpaid boleto or Pix,
    /// <c>payment_intent_payment_attempt_expired</c>), or it failed (a declined card).
    /// </summary>
    public static PaymentStatus FromPaymentIntent(string? status, string? lastErrorCode, bool hasLastError) =>
        status?.ToLowerInvariant() == "requires_payment_method" && hasLastError
            ? lastErrorCode == "payment_intent_payment_attempt_expired" ? PaymentStatus.Expired : PaymentStatus.Failed
            : FromPaymentIntent(status);

    /// <summary>Maps a SetupIntent status to the unified enum.</summary>
    public static SetupStatus FromSetupIntent(string? status) => status?.ToLowerInvariant() switch
    {
        "requires_payment_method" => SetupStatus.Pending,
        "requires_confirmation" => SetupStatus.Pending,
        "requires_action" => SetupStatus.RequiresAction,
        "processing" => SetupStatus.Processing,
        "succeeded" => SetupStatus.Succeeded,
        "canceled" => SetupStatus.Canceled,
        _ => SetupStatus.Unknown,
    };

    /// <summary>Maps a Refund status to the unified enum.</summary>
    public static PaymentStatus FromRefund(string? status) => status?.ToLowerInvariant() switch
    {
        "pending" => PaymentStatus.Processing,
        "succeeded" => PaymentStatus.Refunded,
        "failed" => PaymentStatus.Failed,
        "canceled" => PaymentStatus.Canceled,
        _ => PaymentStatus.Unknown,
    };
}
