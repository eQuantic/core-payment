namespace eQuantic.Payment.Models;

/// <summary>
/// Identifies the payment provider and the API version that produced a response.
/// </summary>
public sealed record ProviderInfo(string Name, string Version)
{
    /// <summary>Unique registration key in the form <c>name@version</c> (e.g. <c>pagarme@v5</c>).</summary>
    public string Key => $"{Name}@{Version}";

    public override string ToString() => Key;
}
