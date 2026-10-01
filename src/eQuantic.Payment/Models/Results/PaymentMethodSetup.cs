namespace eQuantic.Payment.Models.Results;

/// <summary>A card being saved for a customer: what the front end confirms, and what it saved once it did.</summary>
public sealed class PaymentMethodSetup
{
    /// <summary>Provider setup id (e.g. Stripe <c>seti_xxx</c>).</summary>
    public required string Id { get; init; }

    public SetupStatus Status { get; init; }

    /// <summary>Provider's original status string, before normalization.</summary>
    public string? ProviderStatus { get; init; }

    /// <summary>
    /// What a front end confirms the setup with, through the gateway's own SDK (Stripe.js <c>confirmCardSetup</c>).
    /// Hand it to that customer's browser only, and never log it.
    /// </summary>
    public string? ClientSecret { get; init; }

    public string? CustomerId { get; init; }

    /// <summary>The payment method saved, once the setup succeeded (e.g. Stripe <c>pm_xxx</c>).</summary>
    public string? PaymentMethodId { get; init; }

    /// <summary>Why the last attempt to confirm it failed, when one did.</summary>
    public PaymentError? LastError { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }
}

/// <summary>Where a <see cref="PaymentMethodSetup"/> stands.</summary>
public enum SetupStatus
{
    Unknown = 0,

    /// <summary>Waiting for the customer: a card to be entered or the setup confirmed.</summary>
    Pending,

    /// <summary>Waiting for the customer to authenticate the card (3-D Secure).</summary>
    RequiresAction,

    Processing,

    /// <summary>The card is saved: <see cref="PaymentMethodSetup.PaymentMethodId"/> can be charged.</summary>
    Succeeded,

    Canceled,
}
