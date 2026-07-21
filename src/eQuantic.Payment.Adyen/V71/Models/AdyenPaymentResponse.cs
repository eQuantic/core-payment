namespace eQuantic.Payment.Adyen.V71.Models;

// Faithful wire models for the POST /v71/payments (and /payments/details) response, mirroring the reference 1:1.
// The polymorphic action object is modeled as a superset with nullable fields keyed on action.type.

/// <summary>Response of <c>POST /v71/payments</c> and <c>POST /v71/payments/details</c>.</summary>
public sealed class AdyenPaymentResponse
{
    /// <summary>Adyen transaction reference (may be empty when only an <see cref="Action"/> is returned).</summary>
    public string? PspReference { get; set; }

    /// <summary>Outcome code, e.g. <c>Authorised</c>, <c>Pending</c>, <c>PresentToShopper</c>, <c>Refused</c>.</summary>
    public string? ResultCode { get; set; }

    /// <summary>Present when shopper action is required (QR code, voucher, redirect, 3DS).</summary>
    public AdyenAction? Action { get; set; }

    /// <summary>Method/response-specific extras, e.g. <c>pix.expirationDate</c>, <c>cardSummary</c>, <c>authCode</c>.</summary>
    public Dictionary<string, string>? AdditionalData { get; set; }

    /// <summary>Human-readable decline reason (when refused).</summary>
    public string? RefusalReason { get; set; }

    /// <summary>Numeric-as-string decline code (when refused).</summary>
    public string? RefusalReasonCode { get; set; }

    /// <summary>Echoed amount, when present.</summary>
    public AdyenAmount? Amount { get; set; }

    /// <summary>Echo of the request <c>reference</c>.</summary>
    public string? MerchantReference { get; set; }

    /// <summary>Resolved payment method (<c>{ brand, type }</c>), when present.</summary>
    public AdyenResponsePaymentMethod? PaymentMethod { get; set; }
}

/// <summary>
/// Polymorphic action returned when the shopper must act. Modeled as a superset keyed on <see cref="Type"/>:
/// <c>qrCode</c> (PIX), <c>voucher</c> (boleto), <c>redirect</c>/<c>threeDS2</c> (cards).
/// </summary>
public sealed class AdyenAction
{
    /// <summary><c>qrCode</c> | <c>voucher</c> | <c>redirect</c> | <c>threeDS2</c>.</summary>
    public string? Type { get; set; }

    /// <summary>Payment method type this action belongs to, e.g. <c>pix</c>, <c>boletobancario</c>.</summary>
    public string? PaymentMethodType { get; set; }

    /// <summary>PIX: the EMV "copia e cola" / QR payload the shopper scans or pastes.</summary>
    public string? QrCodeData { get; set; }

    /// <summary>Opaque token fed to <c>/payments/details</c> for polling/completion.</summary>
    public string? PaymentData { get; set; }

    /// <summary>Redirect/QR URL, when provided.</summary>
    public string? Url { get; set; }

    /// <summary>Boleto: URL of the printable voucher (PDF).</summary>
    public string? DownloadUrl { get; set; }

    /// <summary>Boleto: expiry timestamp (ISO 8601, no offset), e.g. <c>2019-10-30T00:00:00</c>.</summary>
    public string? ExpiresAt { get; set; }

    /// <summary>Boleto: the barcode / linha digitável.</summary>
    public string? Reference { get; set; }

    /// <summary>Boleto: raw voucher data (barcode fallback), when present.</summary>
    public string? RawData { get; set; }

    /// <summary>Amount associated with the action (e.g. boleto voucher total).</summary>
    public AdyenAmount? TotalAmount { get; set; }
}

/// <summary>Resolved payment method on a response (<c>{ brand, type }</c>).</summary>
public sealed class AdyenResponsePaymentMethod
{
    /// <summary>Card brand, e.g. <c>visa</c>, <c>mc</c>, <c>elo</c>.</summary>
    public string? Brand { get; set; }

    /// <summary>Payment method type, e.g. <c>scheme</c>, <c>pix</c>, <c>boletobancario</c>.</summary>
    public string? Type { get; set; }
}
