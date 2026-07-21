namespace eQuantic.Payment.Models;

/// <summary>
/// Unified payment status. Each provider adapter maps its own status vocabulary into this enum;
/// the original value is always preserved in the raw response.
/// </summary>
public enum PaymentStatus
{
    Unknown = 0,
    /// <summary>Waiting for payment (e.g. PIX not paid yet, boleto not settled).</summary>
    Pending,
    /// <summary>Being processed by the provider/acquirer.</summary>
    Processing,
    /// <summary>Authorized but not captured yet.</summary>
    Authorized,
    /// <summary>Successfully paid/captured.</summary>
    Paid,
    /// <summary>Canceled/voided before settlement.</summary>
    Canceled,
    /// <summary>Refused by the acquirer or failed.</summary>
    Failed,
    /// <summary>Fully refunded.</summary>
    Refunded,
    /// <summary>Partially refunded.</summary>
    PartiallyRefunded,
    /// <summary>Disputed/chargeback.</summary>
    Chargeback,
    /// <summary>Expired without payment (PIX/boleto past due).</summary>
    Expired,
}
