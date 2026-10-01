namespace eQuantic.Payment.Tests.Fakes;

/// <summary>A clock stopped at <paramref name="now"/>.</summary>
public sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
