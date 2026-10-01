namespace eQuantic.Payment.Models.Results;

/// <summary>A payment method the gateway keeps for a customer.</summary>
public sealed class SavedPaymentMethod
{
    /// <summary>Provider payment method id (e.g. Stripe <c>pm_xxx</c>): what <c>CardDetails.PaymentMethodId</c> takes.</summary>
    public required string Id { get; init; }

    /// <summary>The customer it is attached to; null once detached.</summary>
    public string? CustomerId { get; init; }

    public PaymentMethodType Method { get; init; }

    /// <summary>Card brand, e.g. <c>visa</c>, <c>mastercard</c>.</summary>
    public string? Brand { get; init; }

    public string? Last4 { get; init; }

    public int? ExpirationMonth { get; init; }

    public int? ExpirationYear { get; init; }

    /// <summary><c>credit</c>, <c>debit</c>, <c>prepaid</c> or <c>unknown</c>, when the gateway says.</summary>
    public string? Funding { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }
}
