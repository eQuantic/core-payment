namespace eQuantic.Payment.MercadoPago.Payments.Models;

// Faithful wire models for the Mercado Pago Payments API responses, mirroring the official reference/SDK 1:1.
// Deserialized from snake_case. PIX data lives in point_of_interaction.transaction_data; boleto data in
// transaction_details; card data in card.

/// <summary>The <c>payment</c> object (response of <c>POST /v1/payments</c>, capture, cancel, get).</summary>
public sealed class MercadoPagoPaymentResponse
{
    /// <summary>Numeric payment id.</summary>
    public long Id { get; set; }

    public string? Status { get; set; }
    public string? StatusDetail { get; set; }

    public DateTimeOffset? DateCreated { get; set; }
    public DateTimeOffset? DateApproved { get; set; }
    public DateTimeOffset? DateLastUpdated { get; set; }
    public DateTimeOffset? DateOfExpiration { get; set; }
    public DateTimeOffset? MoneyReleaseDate { get; set; }

    public string? OperationType { get; set; }
    public string? PaymentMethodId { get; set; }

    /// <summary><c>bank_transfer</c> (PIX) | <c>ticket</c> (boleto) | <c>credit_card</c> | <c>debit_card</c>.</summary>
    public string? PaymentTypeId { get; set; }

    public string? IssuerId { get; set; }
    public string? CurrencyId { get; set; }
    public string? Description { get; set; }

    public decimal TransactionAmount { get; set; }
    public decimal? TransactionAmountRefunded { get; set; }
    public decimal? NetAmount { get; set; }

    public int? Installments { get; set; }
    public bool? Captured { get; set; }
    public bool? LiveMode { get; set; }
    public string? ExternalReference { get; set; }
    public string? StatementDescriptor { get; set; }
    public long? CollectorId { get; set; }
    public string? AuthorizationCode { get; set; }

    public MercadoPagoPayerResponse? Payer { get; set; }
    public MercadoPagoCardResponse? Card { get; set; }
    public MercadoPagoTransactionDetails? TransactionDetails { get; set; }
    public MercadoPagoPointOfInteraction? PointOfInteraction { get; set; }
    public List<MercadoPagoFeeDetail>? FeeDetails { get; set; }
    public List<MercadoPagoRefundResponse>? Refunds { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class MercadoPagoPayerResponse
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Type { get; set; }
    public string? EntityType { get; set; }
    public MercadoPagoIdentification? Identification { get; set; }
}

public sealed class MercadoPagoCardResponse
{
    public string? Id { get; set; }
    public string? FirstSixDigits { get; set; }
    public string? LastFourDigits { get; set; }
    public int? ExpirationMonth { get; set; }
    public int? ExpirationYear { get; set; }
    public DateTimeOffset? DateCreated { get; set; }
    public DateTimeOffset? DateLastUpdated { get; set; }
    public MercadoPagoCardholder? Cardholder { get; set; }
}

public sealed class MercadoPagoCardholder
{
    public string? Name { get; set; }
    public MercadoPagoIdentification? Identification { get; set; }
}

/// <summary>Boleto/ticket data (<c>ticket</c> payments).</summary>
public sealed class MercadoPagoTransactionDetails
{
    /// <summary>URL to the boleto/ticket page (PDF/HTML).</summary>
    public string? ExternalResourceUrl { get; set; }

    /// <summary>Boleto digitable line (linha digitável).</summary>
    public string? DigitableLine { get; set; }

    public MercadoPagoBarcode? Barcode { get; set; }
    public string? VerificationCode { get; set; }
    public string? FinancialInstitution { get; set; }
    public string? PaymentMethodReferenceId { get; set; }
    public decimal? NetReceivedAmount { get; set; }
    public decimal? TotalPaidAmount { get; set; }
    public decimal? InstallmentAmount { get; set; }
}

public sealed class MercadoPagoBarcode
{
    public string? Content { get; set; }
}

/// <summary>PIX data (<c>bank_transfer</c> payments).</summary>
public sealed class MercadoPagoPointOfInteraction
{
    public string? Type { get; set; }
    public string? SubType { get; set; }
    public MercadoPagoTransactionData? TransactionData { get; set; }
}

public sealed class MercadoPagoTransactionData
{
    /// <summary>PIX "copia e cola" (EMV payload).</summary>
    public string? QrCode { get; set; }

    /// <summary>Base64-encoded PNG of the QR image.</summary>
    public string? QrCodeBase64 { get; set; }

    /// <summary>Hosted PIX payment page.</summary>
    public string? TicketUrl { get; set; }

    public string? TransactionId { get; set; }
}

public sealed class MercadoPagoFeeDetail
{
    public string? Type { get; set; }
    public decimal Amount { get; set; }

    /// <summary><c>collector</c> (seller) | <c>payer</c> (buyer).</summary>
    public string? FeePayer { get; set; }
}

/// <summary>The <c>refund</c> object (response of <c>POST /v1/payments/{id}/refunds</c>, and embedded in <c>payment.refunds[]</c>).</summary>
public sealed class MercadoPagoRefundResponse
{
    public long Id { get; set; }
    public long PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? DateCreated { get; set; }
    public string? RefundMode { get; set; }
    public string? Reason { get; set; }
    public string? UniqueSequenceNumber { get; set; }
}
