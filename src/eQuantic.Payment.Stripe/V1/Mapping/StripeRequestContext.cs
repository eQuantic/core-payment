namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>
/// Context for the PaymentIntent and SetupIntent form mappers. Carries the pinned API version, which names some of
/// the fields, and the reference "today" used to compute the boleto <c>expires_after_days</c> (Stripe expresses
/// boleto expiry in days from today, not a timestamp): São Paulo's today, since Stripe counts a voucher's days there.
/// </summary>
public sealed class StripeRequestContext
{
    public StripeApiVersion Version { get; set; } = StripeDefaults.Version;

    public DateOnly Today { get; set; } = StripeBoleto.Today(DateTimeOffset.UtcNow);
}
