namespace eQuantic.Payment.Models.Requests;

/// <summary>Provider-agnostic refund request.</summary>
public sealed class RefundRequest
{
    /// <summary>Provider charge id returned in <c>Charge.Id</c>.</summary>
    public required string ChargeId { get; init; }

    /// <summary>Amount to refund; omit for a full refund.</summary>
    public Money? Amount { get; init; }
}
