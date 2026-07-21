using eQuantic.Payment.Models;

namespace eQuantic.Payment.MercadoPago;

internal static class MercadoPagoStatusMapper
{
    /// <summary>Maps a Payments API status (+ status_detail) to the unified enum.</summary>
    public static PaymentStatus FromPayments(string? status, string? statusDetail = null)
    {
        if (string.Equals(statusDetail, "expired", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentStatus.Expired;
        }

        if (string.Equals(statusDetail, "partially_refunded", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentStatus.PartiallyRefunded;
        }

        return status?.ToLowerInvariant() switch
        {
            "pending" => PaymentStatus.Pending,
            "approved" => PaymentStatus.Paid,
            "authorized" => PaymentStatus.Authorized,
            "in_process" => PaymentStatus.Processing,
            "in_mediation" => PaymentStatus.Chargeback,
            "rejected" => PaymentStatus.Failed,
            "cancelled" => PaymentStatus.Canceled,
            "refunded" => PaymentStatus.Refunded,
            "charged_back" => PaymentStatus.Chargeback,
            _ => PaymentStatus.Unknown,
        };
    }

    /// <summary>Maps an Orders API status (+ status_detail) to the unified enum.</summary>
    public static PaymentStatus FromOrders(string? status, string? statusDetail = null)
    {
        if (string.Equals(statusDetail, "partially_refunded", StringComparison.OrdinalIgnoreCase))
        {
            return PaymentStatus.PartiallyRefunded;
        }

        return status?.ToLowerInvariant() switch
        {
            "created" => PaymentStatus.Pending,
            "processing" => PaymentStatus.Processing,
            "processed" => PaymentStatus.Paid,
            "action_required" => PaymentStatus.Pending,
            "refunded" => PaymentStatus.Refunded,
            "charged_back" => PaymentStatus.Chargeback,
            "expired" => PaymentStatus.Expired,
            "failed" => PaymentStatus.Failed,
            "canceled" => PaymentStatus.Canceled,
            _ => PaymentStatus.Unknown,
        };
    }
}
