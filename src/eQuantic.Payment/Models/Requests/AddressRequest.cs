namespace eQuantic.Payment.Models.Requests;

/// <summary>
/// Provider-agnostic postal address. Required by some providers for boleto
/// (e.g. Stripe requires full billing address); optional otherwise.
/// </summary>
public sealed class AddressRequest
{
    /// <summary>Street line (street, number, neighborhood).</summary>
    public required string Line1 { get; init; }

    /// <summary>Complement / reference.</summary>
    public string? Line2 { get; init; }

    public required string City { get; init; }

    /// <summary>State code (e.g. <c>SP</c>).</summary>
    public required string State { get; init; }

    /// <summary>Postal code (CEP). Digits or formatted; providers vary.</summary>
    public required string ZipCode { get; init; }

    /// <summary>ISO 3166-1 alpha-2 country code. Defaults to <c>BR</c>.</summary>
    public string Country { get; init; } = "BR";
}
