using eQuantic.Payment.Models;

namespace eQuantic.Payment.Pagarme;

internal static class PagarmeStatusMapper
{
    /// <summary>
    /// Maps a v5 charge to the unified enum. The charge-level <paramref name="chargeStatus"/> only carries
    /// coarse states (pending/paid/canceled/…); authorization, capture and refund nuances live in the
    /// <c>last_transaction.status</c> (<paramref name="transactionStatus"/>), which therefore takes precedence.
    /// </summary>
    public static PaymentStatus FromV5(string? chargeStatus, string? transactionStatus = null)
    {
        // last_transaction.status is the most specific signal when present.
        switch (transactionStatus?.ToLowerInvariant())
        {
            // Card auth/capture
            case "authorized_pending_capture":
            case "waiting_capture":
                return PaymentStatus.Authorized;
            case "captured":
            case "partial_capture":
                return PaymentStatus.Paid;
            case "not_authorized":
            case "with_error":
            case "error_on_voiding":
            case "error_on_refunding":
            case "failed":
                return PaymentStatus.Failed;
            case "voided":
            case "partial_void":
                return PaymentStatus.Canceled;
            case "refunded":
                return PaymentStatus.Refunded;
            case "partial_refunded":
                return PaymentStatus.PartiallyRefunded;
            case "waiting_cancellation":
            case "pending_refund":
                return PaymentStatus.Processing;

            // PIX / boleto lifecycle
            case "waiting_payment":
            case "generated":
            case "viewed":
                return PaymentStatus.Pending;
            case "paid":
                return PaymentStatus.Paid;
            case "underpaid":
                return PaymentStatus.Pending;
            case "overpaid":
                return PaymentStatus.Paid;
        }

        return chargeStatus?.ToLowerInvariant() switch
        {
            "pending" => PaymentStatus.Pending,
            "processing" => PaymentStatus.Processing,
            "paid" => PaymentStatus.Paid,
            "canceled" => PaymentStatus.Canceled,
            "failed" => PaymentStatus.Failed,
            "overpaid" => PaymentStatus.Paid,
            "underpaid" => PaymentStatus.Pending,
            "chargedback" => PaymentStatus.Chargeback,
            _ => PaymentStatus.Unknown,
        };
    }

    /// <summary>Maps a v4 transaction status to the unified enum.</summary>
    public static PaymentStatus FromV4(string? status) => status?.ToLowerInvariant() switch
    {
        "processing" => PaymentStatus.Processing,
        "analyzing" => PaymentStatus.Processing,
        "pending_review" => PaymentStatus.Processing,
        "authorized" => PaymentStatus.Authorized,
        "paid" => PaymentStatus.Paid,
        "waiting_payment" => PaymentStatus.Pending,
        "refunded" => PaymentStatus.Refunded,
        "pending_refund" => PaymentStatus.PartiallyRefunded,
        "refused" => PaymentStatus.Failed,
        "chargedback" => PaymentStatus.Chargeback,
        _ => PaymentStatus.Unknown,
    };
}
