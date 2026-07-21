namespace eQuantic.Payment.Models.Requests;

/// <summary>Provider-agnostic charge creation request.</summary>
public sealed class CreateChargeRequest
{
    public required Money Amount { get; init; }

    public required PaymentMethodType Method { get; init; }

    public CustomerRequest? Customer { get; init; }

    /// <summary>Required when <see cref="Method"/> is credit/debit card.</summary>
    public CardDetails? Card { get; init; }

    /// <summary>Optional PIX settings; defaults apply when omitted.</summary>
    public PixDetails? Pix { get; init; }

    /// <summary>Optional boleto settings; defaults apply when omitted.</summary>
    public BoletoDetails? Boleto { get; init; }

    public string? Description { get; init; }

    /// <summary>Your own correlation id (order id), forwarded to the provider when supported.</summary>
    public string? ReferenceId { get; init; }

    /// <summary>When <c>false</c>, card charges are only authorized and must be captured later.</summary>
    public bool Capture { get; init; } = true;

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>Card data (or a previously tokenized card).</summary>
public sealed class CardDetails
{
    /// <summary>Provider card/payment-method token. When set, the raw card fields are ignored.</summary>
    public string? Token { get; init; }

    public string? Number { get; init; }
    public string? HolderName { get; init; }
    public int ExpirationMonth { get; init; }
    public int ExpirationYear { get; init; }
    public string? Cvv { get; init; }

    /// <summary>Number of installments (parcelas). Defaults to 1.</summary>
    public int Installments { get; init; } = 1;
}

public sealed class PixDetails
{
    /// <summary>How long the PIX QR code stays payable. Default: 1 hour.</summary>
    public TimeSpan ExpiresIn { get; init; } = TimeSpan.FromHours(1);
}

public sealed class BoletoDetails
{
    public DateOnly? DueDate { get; init; }
    public string? Instructions { get; init; }
}
