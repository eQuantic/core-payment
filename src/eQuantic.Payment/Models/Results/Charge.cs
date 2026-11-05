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

    /// <summary>The Multibanco voucher, when <see cref="Method"/> is <see cref="PaymentMethodType.Multibanco"/>.</summary>
    public MultibancoOutput? Multibanco { get; init; }

    /// <summary>
    /// What the customer's browser confirms the charge with, through the gateway's own SDK (Stripe.js
    /// <c>confirmPayment</c>, with the Payment Element): set only on the charge a create returns for a card charge
    /// the browser confirms, and null on every other charge, one read again or carried by a notification included.
    /// Hand it to that customer's browser only, and never store or log it.
    /// </summary>
    public string? ClientSecret { get; init; }

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

/// <summary>
/// A Multibanco voucher: what the customer pays at an ATM or in online banking, the amount being the charge's.
/// </summary>
public sealed class MultibancoOutput
{
    /// <summary>The entity number (<c>entidade</c>).</summary>
    public string? Entity { get; init; }

    /// <summary>The reference number (<c>referência</c>).</summary>
    public string? Reference { get; init; }

    /// <summary>The moment the voucher stops being payable; null once the gateway no longer shows it, as after payment.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>URL of the page the gateway hosts for the voucher; null once the gateway no longer shows it.</summary>
    public string? HostedUrl { get; init; }
}

/// <summary>Card payment artifacts.</summary>
public sealed class CardOutput
{
    public string? Brand { get; init; }
    public string? Last4 { get; init; }
    public string? AuthorizationCode { get; init; }
    public int Installments { get; init; } = 1;
}
