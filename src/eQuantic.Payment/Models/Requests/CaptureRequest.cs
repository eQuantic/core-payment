namespace eQuantic.Payment.Models.Requests;

/// <summary>Provider-agnostic capture of a card charge that was only authorized.</summary>
public sealed class CaptureRequest
{
    /// <summary>Provider charge id returned in <c>Charge.Id</c>.</summary>
    public required string ChargeId { get; init; }

    /// <summary>Amount to capture; omit to capture what was authorized.</summary>
    public Money? Amount { get; init; }

    /// <summary>
    /// The key the gateway recognizes a retry of this request by, answering it with the first one's result
    /// instead of acting twice. Derive it from your own id for the attempt, so a retry after a timeout carries
    /// the same key; at most 64 characters. Gateways without idempotency keys ignore it, and those that need
    /// one send a fresh key when it is omitted.
    /// </summary>
    public string? IdempotencyKey { get; init; }
}
