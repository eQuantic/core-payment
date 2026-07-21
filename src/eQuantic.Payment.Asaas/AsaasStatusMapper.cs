using eQuantic.Payment.Models;

namespace eQuantic.Payment.Asaas;

/// <summary>
/// Maps Asaas payment status strings to the unified <see cref="PaymentStatus"/>. Unrecognized values fall back to
/// <see cref="PaymentStatus.Unknown"/> so a future/unseen Asaas status never breaks the mapping.
/// </summary>
internal static class AsaasStatusMapper
{
    public static PaymentStatus FromPayment(string? status) => status?.ToUpperInvariant() switch
    {
        "PENDING" => PaymentStatus.Pending,
        "AWAITING_RISK_ANALYSIS" => PaymentStatus.Processing,
        "AUTHORIZED" => PaymentStatus.Authorized,
        "CONFIRMED" => PaymentStatus.Paid,
        "RECEIVED" => PaymentStatus.Paid,
        "RECEIVED_IN_CASH" => PaymentStatus.Paid,
        "OVERDUE" => PaymentStatus.Pending,
        "REFUND_REQUESTED" => PaymentStatus.Processing,
        "REFUNDED" => PaymentStatus.Refunded,
        "CHARGEBACK_REQUESTED" => PaymentStatus.Chargeback,
        "CHARGEBACK_DISPUTE" => PaymentStatus.Chargeback,
        _ => PaymentStatus.Unknown,
    };
}
