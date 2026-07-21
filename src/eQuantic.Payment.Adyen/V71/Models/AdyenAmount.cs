namespace eQuantic.Payment.Adyen.V71.Models;

// Faithful wire model for the Adyen Checkout API, mirroring the reference 1:1.
// Serialized as camelCase; nulls omitted. Money is an integer in minor units in amount.value.

/// <summary>Monetary amount. <see cref="Value"/> is an integer in minor units (e.g. R$ 100.00 = <c>10000</c>).</summary>
public sealed class AdyenAmount
{
    /// <summary>Amount in minor units (cents/centavos).</summary>
    public long Value { get; set; }

    /// <summary>3-character ISO 4217 currency code, e.g. <c>BRL</c>.</summary>
    public string Currency { get; set; } = "BRL";
}
