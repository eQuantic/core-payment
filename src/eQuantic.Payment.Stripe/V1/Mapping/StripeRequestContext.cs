namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>
/// Context for the PaymentIntent form mapper. Carries the reference "today" used to compute the
/// boleto <c>expires_after_days</c> (Stripe expresses boleto expiry in days from today, not a timestamp).
/// </summary>
public sealed class StripeRequestContext
{
    public DateOnly Today { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}
