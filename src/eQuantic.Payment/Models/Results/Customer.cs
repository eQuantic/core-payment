namespace eQuantic.Payment.Models.Results;

/// <summary>Unified customer, regardless of provider or API version.</summary>
public sealed class Customer
{
    /// <summary>Provider customer id (e.g. Pagar.me <c>cus_xxx</c>, Stripe <c>cus_xxx</c>).</summary>
    public required string Id { get; init; }

    public string? Name { get; init; }
    public string? Email { get; init; }

    /// <summary>CPF/CNPJ, when the provider stores it.</summary>
    public string? Document { get; init; }
}
