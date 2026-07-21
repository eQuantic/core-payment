namespace eQuantic.Payment.Stripe;

public static class StripeDefaults
{
    public const string ProviderName = "stripe";

    public const string BaseUrl = "https://api.stripe.com/v1/";

    public const string HttpClientName = "eQuantic.Payment.Stripe.V1";

    /// <summary>The dated header value pinned for each modeled version.</summary>
    public static string StripeVersionHeader(StripeApiVersion version) => version switch
    {
        StripeApiVersion.V2024_06_20 => "2024-06-20",
        StripeApiVersion.V2025_04_30_Basil => "2025-04-30.basil",
        _ => throw new ArgumentOutOfRangeException(nameof(version)),
    };

    /// <summary>The version identifier surfaced in <c>ProviderInfo</c> (e.g. <c>stripe@2025-04-30.basil</c>).</summary>
    public static string VersionString(StripeApiVersion version) => StripeVersionHeader(version);
}
