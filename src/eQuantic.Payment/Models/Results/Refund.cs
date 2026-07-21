namespace eQuantic.Payment.Models.Results;

/// <summary>Unified refund, regardless of provider or API version.</summary>
public sealed class Refund
{
    /// <summary>Provider refund id, when the provider issues one (Stripe <c>re_xxx</c>); otherwise the charge id.</summary>
    public required string Id { get; init; }

    public required string ChargeId { get; init; }

    public Money? Amount { get; init; }

    public PaymentStatus Status { get; init; }

    /// <summary>Provider's original status string, before normalization.</summary>
    public string? ProviderStatus { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }
}
