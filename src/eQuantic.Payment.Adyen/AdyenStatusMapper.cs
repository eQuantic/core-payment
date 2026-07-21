using eQuantic.Payment.Models;

namespace eQuantic.Payment.Adyen;

/// <summary>Maps an Adyen <c>resultCode</c> to the unified <see cref="PaymentStatus"/>.</summary>
internal static class AdyenStatusMapper
{
    /// <summary>
    /// Maps a payment <c>resultCode</c> (from <c>/payments</c> and <c>/payments/details</c>) to the unified enum.
    /// Unrecognized values fall back to <see cref="PaymentStatus.Unknown"/>.
    /// </summary>
    public static PaymentStatus FromResultCode(string? resultCode) => resultCode?.ToLowerInvariant() switch
    {
        "authorised" => PaymentStatus.Paid,
        "received" => PaymentStatus.Processing,
        "pending" => PaymentStatus.Pending,
        "presenttoshopper" => PaymentStatus.Pending,
        "redirectshopper" => PaymentStatus.Pending,
        "identifyshopper" => PaymentStatus.Pending,
        "challengeshopper" => PaymentStatus.Pending,
        "refused" => PaymentStatus.Failed,
        "error" => PaymentStatus.Failed,
        "cancelled" => PaymentStatus.Canceled,
        _ => PaymentStatus.Unknown,
    };

    /// <summary>
    /// <c>true</c> when a (successful HTTP 200) response carries a declined/failed outcome
    /// (<c>Refused</c> or <c>Error</c>) that must be surfaced as a failed <c>PaymentResponse</c>.
    /// </summary>
    public static bool IsRefusal(string? resultCode)
        => resultCode?.ToLowerInvariant() is "refused" or "error";
}
