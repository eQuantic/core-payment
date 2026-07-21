namespace eQuantic.Payment.Asaas.V3.Models;

// Faithful wire models for Asaas v3 responses, mirroring the official reference 1:1. Deserialized from camelCase.
// Response payloads are supersets (many fields are null depending on billingType/state), so fields are broadly
// nullable. Date/date-time fields are kept as strings ("yyyy-MM-dd" or "yyyy-MM-dd HH:mm:ss") for safe 1:1
// round-tripping; the mappers parse them where a typed value is needed.

/// <summary>The <c>payment</c> object (response of create / get / capture / refund).</summary>
public sealed class AsaasPaymentResponse
{
    public string Id { get; set; } = string.Empty;
    public string? DateCreated { get; set; }
    public string? Customer { get; set; }
    public string? Subscription { get; set; }
    public string? Installment { get; set; }
    public string? PaymentLink { get; set; }

    public decimal? Value { get; set; }
    public decimal? NetValue { get; set; }
    public decimal? OriginalValue { get; set; }
    public decimal? InterestValue { get; set; }

    public string? Description { get; set; }

    /// <summary><c>PIX</c> | <c>BOLETO</c> | <c>CREDIT_CARD</c> | <c>DEBIT_CARD</c> | <c>UNDEFINED</c>.</summary>
    public string? BillingType { get; set; }

    public AsaasCreditCardResponse? CreditCard { get; set; }

    /// <summary>Payment status (see the Asaas status vocabulary).</summary>
    public string? Status { get; set; }

    public string? DueDate { get; set; }
    public string? OriginalDueDate { get; set; }
    public string? PaymentDate { get; set; }
    public string? ClientPaymentDate { get; set; }

    public int? InstallmentNumber { get; set; }

    /// <summary>Hosted invoice/checkout page.</summary>
    public string? InvoiceUrl { get; set; }
    public string? InvoiceNumber { get; set; }

    public string? ExternalReference { get; set; }

    public bool Deleted { get; set; }
    public bool Anticipated { get; set; }
    public bool Anticipable { get; set; }

    public string? TransactionReceiptUrl { get; set; }

    /// <summary>Boleto "nosso número".</summary>
    public string? NossoNumero { get; set; }

    /// <summary>Boleto PDF download URL.</summary>
    public string? BankSlipUrl { get; set; }

    public bool PostalService { get; set; }

    public List<AsaasRefundItem>? Refunds { get; set; }
}

/// <summary>Card data echoed in the payment response (<c>creditCard</c>).</summary>
public sealed class AsaasCreditCardResponse
{
    /// <summary>Last four digits only.</summary>
    public string? CreditCardNumber { get; set; }

    /// <summary><c>VISA</c> | <c>MASTERCARD</c> | <c>ELO</c> | <c>AMEX</c> | …</summary>
    public string? CreditCardBrand { get; set; }

    /// <summary>Token for reusing this card on future charges.</summary>
    public string? CreditCardToken { get; set; }
}

/// <summary>A refund entry in <c>payment.refunds[]</c> (populated after the refund endpoint is called).</summary>
public sealed class AsaasRefundItem
{
    public string? DateCreated { get; set; }
    public string? Status { get; set; }
    public decimal? Value { get; set; }
    public string? EndToEndIdentifier { get; set; }
    public string? EffectiveDate { get; set; }
    public string? Description { get; set; }
    public string? TransactionReceiptUrl { get; set; }
}

/// <summary>Response of <c>DELETE /payments/{id}</c> (<c>{ "deleted": true, "id": "pay_..." }</c>).</summary>
public sealed class AsaasDeleteResponse
{
    public bool Deleted { get; set; }
    public string Id { get; set; } = string.Empty;
}

/// <summary>Response of <c>GET /payments/{id}/pixQrCode</c>.</summary>
public sealed class AsaasPixQrCodeResponse
{
    /// <summary>QR code PNG as base64 (no <c>data:</c> prefix).</summary>
    public string? EncodedImage { get; set; }

    /// <summary>PIX "copia e cola" (EMV payload).</summary>
    public string? Payload { get; set; }

    /// <summary>Expiration date-time in <c>yyyy-MM-dd HH:mm:ss</c>.</summary>
    public string? ExpirationDate { get; set; }

    public string? Description { get; set; }
}

/// <summary>Response of <c>GET /payments/{id}/identificationField</c>.</summary>
public sealed class AsaasIdentificationFieldResponse
{
    /// <summary>Boleto digitable line (linha digitável).</summary>
    public string? IdentificationField { get; set; }

    public string? NossoNumero { get; set; }

    /// <summary>Boleto barcode numeric representation.</summary>
    public string? BarCode { get; set; }
}

/// <summary>
/// Internal aggregation used as the mapping source for the unified
/// <see cref="eQuantic.Payment.Models.Results.Charge"/>: the base payment plus the follow-up PIX QR and boleto
/// identification-field responses (Asaas exposes those as separate GET calls, not on the payment object).
/// This is a composition model, not an Asaas wire object.
/// </summary>
public sealed class AsaasPaymentResult
{
    public required AsaasPaymentResponse Payment { get; init; }
    public AsaasPixQrCodeResponse? PixQrCode { get; init; }
    public AsaasIdentificationFieldResponse? IdentificationField { get; init; }
}
