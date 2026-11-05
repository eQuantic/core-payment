namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>Context for <see cref="StripeCreatedChargeMapper"/>: what the create the PaymentIntent answers was for.</summary>
public sealed class StripeChargeContext
{
    /// <summary>
    /// Whether the customer's browser confirms the charge (a card charge with no card to charge), so the charge carries
    /// the PaymentIntent's client secret for it.
    /// </summary>
    public bool ConfirmsInBrowser { get; set; }
}
