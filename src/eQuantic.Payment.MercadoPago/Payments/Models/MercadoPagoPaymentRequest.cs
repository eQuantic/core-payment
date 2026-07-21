namespace eQuantic.Payment.MercadoPago.Payments.Models;

// Faithful wire models for the Mercado Pago Payments API request (POST /v1/payments),
// mirroring the official reference/SDK 1:1. Serialized as snake_case; null properties omitted.
// Note: amounts are decimal reais (NOT centavos), unlike most gateways.

/// <summary>Request body for <c>POST /v1/payments</c>.</summary>
public sealed class MercadoPagoPaymentRequest
{
    /// <summary>Total amount, in decimal currency units (e.g. <c>150.00</c>), not centavos.</summary>
    public decimal TransactionAmount { get; set; }

    /// <summary>Method id: <c>pix</c>, <c>bolbradesco</c> (boleto), card brand (<c>visa</c>, <c>master</c>, <c>debvisa</c>, …).</summary>
    public string PaymentMethodId { get; set; } = string.Empty;

    public MercadoPagoPayerRequest Payer { get; set; } = new();

    /// <summary>Card token (cards only).</summary>
    public string? Token { get; set; }

    /// <summary>Installments (cards); ≥ 1.</summary>
    public int? Installments { get; set; }

    public string? IssuerId { get; set; }
    public string? Description { get; set; }
    public string? ExternalReference { get; set; }

    /// <summary><c>false</c> authorizes only (cards); default <c>true</c>.</summary>
    public bool? Capture { get; set; }

    /// <summary>When <c>true</c>, resolves only to <c>approved</c>/<c>rejected</c> (no intermediate states).</summary>
    public bool? BinaryMode { get; set; }

    public string? NotificationUrl { get; set; }

    /// <summary>PIX/boleto expiry (ISO 8601 with offset).</summary>
    public DateTimeOffset? DateOfExpiration { get; set; }

    public string? StatementDescriptor { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class MercadoPagoPayerRequest
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary><c>customer</c> | <c>guest</c>.</summary>
    public string? Type { get; set; }

    /// <summary>Existing customer id (saved-card flows).</summary>
    public string? Id { get; set; }

    /// <summary><c>individual</c> | <c>association</c>.</summary>
    public string? EntityType { get; set; }

    public MercadoPagoIdentification? Identification { get; set; }
    public MercadoPagoPayerAddress? Address { get; set; }
    public MercadoPagoPhone? Phone { get; set; }
}

public sealed class MercadoPagoIdentification
{
    /// <summary><c>CPF</c> | <c>CNPJ</c>.</summary>
    public string? Type { get; set; }
    public string? Number { get; set; }
}

public sealed class MercadoPagoPayerAddress
{
    public string? ZipCode { get; set; }
    public string? StreetName { get; set; }
    public int? StreetNumber { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }

    /// <summary>UF (e.g. <c>SP</c>).</summary>
    public string? FederalUnit { get; set; }
}

public sealed class MercadoPagoPhone
{
    public string? AreaCode { get; set; }
    public string? Number { get; set; }
}

/// <summary>Request body for capture via <c>PUT /v1/payments/{id}</c>.</summary>
public sealed class MercadoPagoCaptureRequest
{
    public bool Capture { get; set; } = true;

    /// <summary>Optional partial-capture amount (decimal reais).</summary>
    public decimal? TransactionAmount { get; set; }
}

/// <summary>Request body for cancel via <c>PUT /v1/payments/{id}</c>.</summary>
public sealed class MercadoPagoCancelRequest
{
    public string Status { get; set; } = "cancelled";
}

/// <summary>Request body for <c>POST /v1/payments/{id}/refunds</c> (omit amount for a full refund).</summary>
public sealed class MercadoPagoRefundRequest
{
    public decimal? Amount { get; set; }
}
