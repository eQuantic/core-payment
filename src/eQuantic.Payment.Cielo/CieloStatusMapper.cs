using eQuantic.Payment.Models;

namespace eQuantic.Payment.Cielo;

/// <summary>Maps Cielo's numeric <c>Payment.Status</c> to the unified <see cref="PaymentStatus"/>.</summary>
internal static class CieloStatusMapper
{
    /// <summary>
    /// Maps a Cielo numeric status code to the unified enum. Codes are sparse (0,1,2,3,10,11,12,13,20);
    /// unknown values fall back to <see cref="PaymentStatus.Unknown"/>.
    /// </summary>
    public static PaymentStatus FromCode(int status) => status switch
    {
        0 => PaymentStatus.Pending,        // NotFinished
        1 => PaymentStatus.Authorized,     // Authorized
        2 => PaymentStatus.Paid,           // PaymentConfirmed
        3 => PaymentStatus.Failed,         // Denied
        10 => PaymentStatus.Canceled,      // Voided
        11 => PaymentStatus.Refunded,      // Refunded
        12 => PaymentStatus.Pending,       // Pending
        13 => PaymentStatus.Failed,        // Aborted
        20 => PaymentStatus.Pending,       // Scheduled
        _ => PaymentStatus.Unknown,
    };
}
