using eQuantic.Payment.MercadoPago.Payments.Models;

namespace eQuantic.Payment.MercadoPago.Orders.Models;

// Faithful wire models for the Mercado Pago Orders API request (POST /v1/orders), mirroring the reference 1:1.
// Serialized as snake_case; null omitted. NOTE: monetary amounts are STRINGS (e.g. "150.00"), not numbers.

/// <summary>Request body for <c>POST /v1/orders</c>.</summary>
public sealed class MercadoPagoOrderRequest
{
    /// <summary>For online payments: <c>online</c>.</summary>
    public string Type { get; set; } = "online";

    /// <summary>Total amount as a string, e.g. <c>"150.00"</c>.</summary>
    public string TotalAmount { get; set; } = string.Empty;

    public string? ExternalReference { get; set; }

    /// <summary><c>automatic</c> (single stage) | <c>manual</c> (staged capture).</summary>
    public string? ProcessingMode { get; set; }

    /// <summary><c>automatic</c> | <c>manual</c>.</summary>
    public string? CaptureMode { get; set; }

    public string? Currency { get; set; }
    public string? Description { get; set; }

    public MercadoPagoOrderTransactionsRequest Transactions { get; set; } = new();
    public MercadoPagoOrderPayerRequest? Payer { get; set; }
}

public sealed class MercadoPagoOrderTransactionsRequest
{
    public List<MercadoPagoOrderPaymentRequest> Payments { get; set; } = [];
}

public sealed class MercadoPagoOrderPaymentRequest
{
    public string Amount { get; set; } = string.Empty;
    public MercadoPagoOrderPaymentMethodRequest PaymentMethod { get; set; } = new();

    /// <summary>Absolute expiry (PIX/boleto).</summary>
    public DateTimeOffset? DateOfExpiration { get; set; }
}

public sealed class MercadoPagoOrderPaymentMethodRequest
{
    /// <summary><c>pix</c>, <c>bolbradesco</c>, card brand (<c>visa</c>, …).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary><c>bank_transfer</c> (PIX) | <c>ticket</c> (boleto) | <c>credit_card</c> | <c>debit_card</c>.</summary>
    public string Type { get; set; } = string.Empty;

    public string? Token { get; set; }
    public int? Installments { get; set; }
    public string? StatementDescriptor { get; set; }
}

public sealed class MercadoPagoOrderPayerRequest
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary><c>individual</c> | <c>association</c>.</summary>
    public string? EntityType { get; set; }

    public string? CustomerId { get; set; }
    public MercadoPagoIdentification? Identification { get; set; }
    public MercadoPagoPhone? Phone { get; set; }
    public MercadoPagoPayerAddress? Address { get; set; }
}

/// <summary>Request body for <c>POST /v1/orders/{id}/refund</c> (omit for full refund).</summary>
public sealed class MercadoPagoOrderRefundRequest
{
    /// <summary>Partial refund amount as a string; omit for a full refund.</summary>
    public string? Amount { get; set; }
}
