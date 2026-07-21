using eQuantic.Payment.Models;

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
