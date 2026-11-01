namespace eQuantic.Payment.Stripe;

public static class StripeDefaults
{
    public const string ProviderName = "stripe";

    public const string BaseUrl = "https://api.stripe.com/v1/";

    public const string HttpClientName = "eQuantic.Payment.Stripe.V1";

    /// <summary>
    /// How far an event's signing time may be from now before it is refused as a replay: five minutes, the
    /// tolerance Stripe's own libraries apply.
    /// </summary>
    public static readonly TimeSpan WebhookTolerance = TimeSpan.FromMinutes(5);

    /// <summary>The version pinned when the options name none.</summary>
    internal const StripeApiVersion Version = StripeApiVersion.V2025_04_30_Basil;

    /// <summary>The dated header value pinned for each modeled version.</summary>
    public static string StripeVersionHeader(StripeApiVersion version) => version switch
    {
        StripeApiVersion.V2024_06_20 => "2024-06-20",
        StripeApiVersion.V2025_04_30_Basil => "2025-04-30.basil",
        StripeApiVersion.V2026_09_30_Endive => "2026-09-30.endive",
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    /// <summary>
    /// The form field a PaymentIntent or a SetupIntent names the payment method types it accepts in. Endive answers
    /// 400 (<c>payment_method_types_no_longer_supported</c>) to <c>payment_method_types</c> and takes
    /// <c>allowed_payment_method_types</c>, which the versions before Dahlia (2026-07-29) do not know.
    /// </summary>
    internal static string PaymentMethodTypesField(StripeApiVersion version) => version switch
    {
        StripeApiVersion.V2024_06_20 or StripeApiVersion.V2025_04_30_Basil => "payment_method_types[]",
        StripeApiVersion.V2026_09_30_Endive => "allowed_payment_method_types[]",
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    /// <summary>The version identifier surfaced in <c>ProviderInfo</c> (e.g. <c>stripe@2025-04-30.basil</c>).</summary>
    public static string VersionString(StripeApiVersion version) => StripeVersionHeader(version);
}
