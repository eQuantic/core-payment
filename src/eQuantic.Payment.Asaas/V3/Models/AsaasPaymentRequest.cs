namespace eQuantic.Payment.Asaas.V3.Models;

// Faithful wire models for the Asaas v3 payment request (POST /payments), mirroring the official reference 1:1.
// Serialized as camelCase; null properties are omitted. Monetary values are decimal reais (NOT centavos).

/// <summary>Request body for <c>POST /payments</c>. Required: <c>customer</c>, <c>billingType</c>, <c>value</c>, <c>dueDate</c>.</summary>
public sealed class AsaasPaymentRequest
{
    /// <summary>Asaas customer id (e.g. <c>cus_000005219613</c>); injected after the customer is created.</summary>
    public string? Customer { get; set; }

    /// <summary><c>PIX</c> | <c>BOLETO</c> | <c>CREDIT_CARD</c>.</summary>
    public string BillingType { get; set; } = string.Empty;

    /// <summary>Charge amount in decimal reais (e.g. <c>150.00</c>), not centavos.</summary>
    public decimal Value { get; set; }

    /// <summary>Due date in <c>yyyy-MM-dd</c>.</summary>
    public string DueDate { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Your own correlation id, echoed back on the payment.</summary>
    public string? ExternalReference { get; set; }

    /// <summary>Number of installments (cards). Sent when greater than one.</summary>
    public int? InstallmentCount { get; set; }

    /// <summary>Per-installment amount (alternative to <see cref="TotalValue"/>).</summary>
    public decimal? InstallmentValue { get; set; }

    /// <summary>Total to auto-split across installments (alternative to <see cref="InstallmentValue"/>).</summary>
    public decimal? TotalValue { get; set; }

    /// <summary>Card only: pre-authorize without capturing (the charge ends in <c>AUTHORIZED</c>).</summary>
    public bool? AuthorizeOnly { get; set; }

    /// <summary>Card only: payer IP address (anti-fraud). Required by Asaas for raw-card charges.</summary>
    public string? RemoteIp { get; set; }

    /// <summary>Card only: reuse a previously tokenized card instead of raw card data.</summary>
    public string? CreditCardToken { get; set; }

    public AsaasCreditCard? CreditCard { get; set; }

    public AsaasCreditCardHolderInfo? CreditCardHolderInfo { get; set; }
}

/// <summary>Raw card data (<c>creditCard</c>) for a card charge.</summary>
public sealed class AsaasCreditCard
{
    public string? HolderName { get; set; }

    /// <summary>Full PAN (digits only).</summary>
    public string? Number { get; set; }

    /// <summary>2-digit expiry month (e.g. <c>05</c>).</summary>
    public string? ExpiryMonth { get; set; }

    /// <summary>4-digit expiry year (e.g. <c>2028</c>).</summary>
    public string? ExpiryYear { get; set; }

    /// <summary>Security code (CVV). Asaas names this key <c>ccv</c>.</summary>
    public string? Ccv { get; set; }
}

/// <summary>Card holder information (<c>creditCardHolderInfo</c>), required for raw-card charges.</summary>
public sealed class AsaasCreditCardHolderInfo
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? CpfCnpj { get; set; }
    public string? PostalCode { get; set; }
    public string? AddressNumber { get; set; }
    public string? AddressComplement { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
}

/// <summary>Request body for <c>POST /payments/{id}/refund</c> (omit <c>value</c> for a full refund).</summary>
public sealed class AsaasRefundRequest
{
    /// <summary>Partial refund amount in decimal reais; omit for a full refund.</summary>
    public decimal? Value { get; set; }

    public string? Description { get; set; }
}
