using eQuantic.Payment.MercadoPago.Payments.Models;

namespace eQuantic.Payment.MercadoPago.Orders.Models;

// Faithful wire models for the Mercado Pago Orders API responses, mirroring the reference 1:1.
// PIX/boleto/card output fields are flattened onto transactions.payments[].payment_method.
// Monetary amounts are STRINGS.

/// <summary>The <c>order</c> object (response of <c>POST /v1/orders</c>, get, capture, cancel).</summary>
public sealed class MercadoPagoOrderResponse
{
    /// <summary>Order id, e.g. <c>ORD01ABC...</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? Type { get; set; }
    public string? Status { get; set; }
    public string? StatusDetail { get; set; }
    public string? TotalAmount { get; set; }
    public string? TotalPaidAmount { get; set; }
    public string? ExternalReference { get; set; }
    public string? CountryCode { get; set; }
    public string? Currency { get; set; }
    public string? CaptureMode { get; set; }
    public string? ProcessingMode { get; set; }
    public string? UserId { get; set; }
    public string? CheckoutUrl { get; set; }
    public DateTimeOffset? CreatedDate { get; set; }
    public DateTimeOffset? LastUpdatedDate { get; set; }

    public MercadoPagoOrderTransactionsResponse? Transactions { get; set; }
    public MercadoPagoOrderPayerResponse? Payer { get; set; }
}

public sealed class MercadoPagoOrderTransactionsResponse
{
    public List<MercadoPagoOrderPaymentResponse>? Payments { get; set; }
    public List<MercadoPagoOrderRefundResponse>? Refunds { get; set; }
}

public sealed class MercadoPagoOrderPaymentResponse
{
    /// <summary>Payment id, e.g. <c>PAY01ABC...</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? ReferenceId { get; set; }
    public string? Status { get; set; }
    public string? StatusDetail { get; set; }
    public string? Amount { get; set; }
    public string? PaidAmount { get; set; }
    public string? RefundedAmount { get; set; }
    public DateTimeOffset? DateOfExpiration { get; set; }
    public int? AttemptNumber { get; set; }
    public string? Provider { get; set; }
    public MercadoPagoOrderPaymentMethodResponse? PaymentMethod { get; set; }
}

/// <summary>Flattened payment-method output — PIX/boleto/card artifacts live here.</summary>
public sealed class MercadoPagoOrderPaymentMethodResponse
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? CardId { get; set; }
    public int? Installments { get; set; }
    public string? StatementDescriptor { get; set; }

    // PIX
    public string? QrCode { get; set; }
    public string? QrCodeBase64 { get; set; }
    public string? TicketUrl { get; set; }
    public string? E2eId { get; set; }

    // Boleto
    public string? DigitableLine { get; set; }
    public string? BarcodeContent { get; set; }
    public string? VerificationCode { get; set; }
    public string? FinancialInstitution { get; set; }
}

public sealed class MercadoPagoOrderRefundResponse
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public string? Amount { get; set; }
    public string? ReferenceId { get; set; }
    public string? TransactionId { get; set; }
}

public sealed class MercadoPagoOrderPayerResponse
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? CustomerId { get; set; }
    public MercadoPagoIdentification? Identification { get; set; }
}
