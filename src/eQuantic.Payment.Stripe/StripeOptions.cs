namespace eQuantic.Payment.Stripe;

/// <summary>
/// Stripe API versions. Stripe ships a single REST major (<c>v1</c>) but pins behavior through
/// the dated <c>Stripe-Version</c> header — that date is the real version axis, so it is modeled here.
/// </summary>
public enum StripeApiVersion
{
    /// <summary>Pins <c>Stripe-Version: 2024-06-20</c>.</summary>
    V2024_06_20,

    /// <summary>Pins <c>Stripe-Version: 2025-04-30.basil</c> (the "Basil" release).</summary>
    V2025_04_30_Basil,
}

public sealed class StripeOptions
{
    /// <summary>Secret key: <c>sk_live_xxx</c> / <c>sk_test_xxx</c>, or a restricted key <c>rk_xxx</c>.</summary>
    public required string ApiKey { get; set; }

    /// <summary>Dated API version pinned via the <c>Stripe-Version</c> header.</summary>
    public StripeApiVersion Version { get; set; } = StripeApiVersion.V2025_04_30_Basil;

    /// <summary>Override the base URL (proxy/mock). Defaults to <c>https://api.stripe.com/v1/</c>.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>
    /// The signing secret of the webhook endpoint (<c>whsec_xxx</c>), which verifies the events Stripe sends to
    /// it. Without it, every notification is refused.
    /// </summary>
    public string? WebhookSecret { get; set; }

    /// <summary>How far an event's signing time may be from now before it is refused as a replay.</summary>
    public TimeSpan WebhookTolerance { get; set; } = StripeDefaults.WebhookTolerance;
}
