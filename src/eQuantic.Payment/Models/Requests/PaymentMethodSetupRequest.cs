namespace eQuantic.Payment.Models.Requests;

/// <summary>Starts saving a card for a customer, to be charged later with the customer away.</summary>
public sealed class PaymentMethodSetupRequest
{
    /// <summary>The gateway's id of the customer the card is saved for (<c>Customers.CreateAsync</c>).</summary>
    public required string CustomerId { get; init; }

    /// <summary>
    /// The key the gateway recognizes a retry of this request by; derive it from your own id for the attempt.
    /// See <see cref="CreateChargeRequest.IdempotencyKey"/>.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}
