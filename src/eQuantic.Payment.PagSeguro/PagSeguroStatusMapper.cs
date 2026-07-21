using eQuantic.Payment.Models;

namespace eQuantic.Payment.PagSeguro;

internal static class PagSeguroStatusMapper
{
    /// <summary>Maps a PagSeguro charge status to the unified enum.</summary>
    public static PaymentStatus FromCharge(string? status) => status?.ToUpperInvariant() switch
    {
        "AUTHORIZED" => PaymentStatus.Authorized,
        "PAID" => PaymentStatus.Paid,
        "IN_ANALYSIS" => PaymentStatus.Processing,
        "DECLINED" => PaymentStatus.Failed,
        "CANCELED" => PaymentStatus.Canceled,
        "WAITING" => PaymentStatus.Pending,
        _ => PaymentStatus.Unknown,
    };
}
