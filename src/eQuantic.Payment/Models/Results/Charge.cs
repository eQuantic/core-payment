namespace eQuantic.Payment.Models.Results;

/// <summary>Unified charge, regardless of provider or API version.</summary>
public sealed class Charge
{
    /// <summary>Provider charge id (e.g. Pagar.me <c>ch_xxx</c>, Stripe <c>pi_xxx</c>).</summary>
    public required string Id { get; init; }

    /// <summary>Your correlation id, when echoed back by the provider.</summary>
    public string? ReferenceId { get; init; }

    public PaymentStatus Status { get; init; }

    /// <summary>Provider's original status string, before normalization.</summary>
    public string? ProviderStatus { get; init; }

    public Money Amount { get; init; }

    public PaymentMethodType Method { get; init; }

    public DateTimeOffset? CreatedAt { get; init; }

    public PixOutput? Pix { get; init; }
    public BoletoOutput? Boleto { get; init; }
    public CardOutput? Card { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

/// <summary>PIX payment artifacts.</summary>
public sealed class PixOutput
{
    /// <summary>EMV payload ("copia e cola").</summary>
    public string? QrCode { get; init; }

    /// <summary>URL of the QR code image, when provided.</summary>
    public string? QrCodeImageUrl { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>Boleto payment artifacts.</summary>
public sealed class BoletoOutput
{
    public string? Barcode { get; init; }

    /// <summary>Digitable line (linha digitável).</summary>
    public string? DigitableLine { get; init; }

    /// <summary>URL of the printable boleto: the PDF when there is one, otherwise the hosted page.</summary>
    public string? Url { get; init; }

    /// <summary>URL of the voucher's PDF.</summary>
    public string? PdfUrl { get; init; }

    /// <summary>URL of the page the gateway hosts for the voucher.</summary>
    public string? HostedUrl { get; init; }

    public DateOnly? DueDate { get; init; }

    /// <summary>The moment the voucher stops being payable.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>Card payment artifacts.</summary>
public sealed class CardOutput
{
    public string? Brand { get; init; }
    public string? Last4 { get; init; }
    public string? AuthorizationCode { get; init; }
    public int Installments { get; init; } = 1;
}
