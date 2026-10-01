namespace eQuantic.Payment.Models.Requests;

/// <summary>Provider-agnostic cancellation of a charge not yet settled.</summary>
public sealed class CancelRequest
{
    /// <summary>Provider charge id returned in <c>Charge.Id</c>.</summary>
    public required string ChargeId { get; init; }

    /// <summary>
    /// The key the gateway recognizes a retry of this request by, answering it with the first one's result
    /// instead of acting twice. Derive it from your own id for the attempt, so a retry after a timeout carries
    /// the same key; at most 64 characters. Gateways without idempotency keys ignore it, and those that need
    /// one send a fresh key when it is omitted.
    /// </summary>
    public string? IdempotencyKey { get; init; }
}
