namespace eQuantic.Payment.Models.Results;

/// <summary>A notification a gateway sent, once verified: what happened, when, and to which object.</summary>
public sealed class PaymentNotification
{
    /// <summary>The gateway's id for the event: the key to deduplicate on, since gateways deliver more than once.</summary>
    public required string Id { get; init; }

    /// <summary>The gateway's name for what happened, e.g. <c>payment_intent.succeeded</c>.</summary>
    public required string Type { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>The id of the object the event is about, e.g. <c>pi_123</c>.</summary>
    public string? ObjectId { get; init; }

    /// <summary>What kind of object that is, e.g. <c>payment_intent</c>.</summary>
    public string? ObjectType { get; init; }

    /// <summary>Whether the event happened in live mode rather than in the gateway's test mode.</summary>
    public bool LiveMode { get; init; }

    /// <summary>The gateway API version the event's object was rendered in, when the gateway says.</summary>
    public string? ApiVersion { get; init; }
}
