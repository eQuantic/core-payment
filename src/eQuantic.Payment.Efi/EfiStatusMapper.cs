using eQuantic.Payment.Models;

namespace eQuantic.Payment.Efi;

internal static class EfiStatusMapper
{
    /// <summary>Maps a Pix charge (<c>cob</c>) status to the unified enum.</summary>
    public static PaymentStatus FromPix(string? status) => status?.ToUpperInvariant() switch
    {
        "ATIVA" => PaymentStatus.Pending,
        "CONCLUIDA" => PaymentStatus.Paid,
        "REMOVIDA_PELO_USUARIO_RECEBEDOR" => PaymentStatus.Canceled,
        "REMOVIDA_PELO_PSP" => PaymentStatus.Canceled,
        _ => PaymentStatus.Unknown,
    };

    /// <summary>Maps a Cobranças charge status to the unified enum.</summary>
    public static PaymentStatus FromCobrancas(string? status) => status?.ToLowerInvariant() switch
    {
        "new" => PaymentStatus.Pending,
        "waiting" => PaymentStatus.Pending,
        "link" => PaymentStatus.Pending,
        "identified" => PaymentStatus.Processing,
        "approved" => PaymentStatus.Authorized,
        "paid" => PaymentStatus.Paid,
        "settled" => PaymentStatus.Paid,
        "unpaid" => PaymentStatus.Failed,
        "refunded" => PaymentStatus.Refunded,
        "contested" => PaymentStatus.Chargeback,
        "canceled" => PaymentStatus.Canceled,
        "expired" => PaymentStatus.Expired,
        _ => PaymentStatus.Unknown,
    };
}
