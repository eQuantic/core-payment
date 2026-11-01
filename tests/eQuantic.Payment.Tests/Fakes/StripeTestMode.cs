namespace eQuantic.Payment.Tests.Fakes;

/// <summary>
/// Stripe's test mode, when the environment has a key for it in <c>STRIPE_SECRET_KEY</c>. Without one, the tests that
/// need it are skipped; with a live key, they fail rather than charge real money.
/// </summary>
public static class StripeTestMode
{
    public const string KeyVariable = "STRIPE_SECRET_KEY";

    public const string SkipReason = $"No Stripe test key: set {KeyVariable} (sk_test_…) in the environment.";

    private static readonly Lazy<string?> Key = new(Find);

    public static bool Available => Key.Value is not null;

    /// <summary>The test key; asked for only by a test that was not skipped.</summary>
    public static string SecretKey => Key.Value ?? throw new InvalidOperationException(SkipReason);

    private static string? Find()
    {
        var key = Environment.GetEnvironmentVariable(KeyVariable)?.Trim();
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        return key.StartsWith("sk_test_", StringComparison.Ordinal)
            ? key
            : throw new InvalidOperationException(
                $"{KeyVariable} is not a test-mode secret key (sk_test_…). These tests charge cards and issue boletos, so they run in Stripe's test mode only.");
    }
}

/// <summary>A fact that runs in Stripe's test mode, and is skipped where <see cref="StripeTestMode"/> has no key.</summary>
public sealed class StripeTestModeFactAttribute : FactAttribute
{
    public StripeTestModeFactAttribute()
    {
        if (!StripeTestMode.Available)
        {
            Skip = StripeTestMode.SkipReason;
        }
    }
}
