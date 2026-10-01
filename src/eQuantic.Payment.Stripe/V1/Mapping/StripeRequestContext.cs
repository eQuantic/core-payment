namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>
/// Context for the PaymentIntent form mapper. Carries the reference "today" used to compute the
/// boleto <c>expires_after_days</c> (Stripe expresses boleto expiry in days from today, not a timestamp):
/// São Paulo's today, since Stripe counts a voucher's days there.
/// </summary>
public sealed class StripeRequestContext
{
    public DateOnly Today { get; set; } = StripeBoleto.Today(DateTimeOffset.UtcNow);
}
