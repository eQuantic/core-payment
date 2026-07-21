using System.Text.Json.Serialization;

namespace eQuantic.Payment.Pagarme.V5.Models;

// Faithful wire models for Pagar.me Core API v5 responses, mirroring the official reference 1:1
// (orders: docs.pagar.me/reference/pedidos-1, charges: cobranças-1, transactions: pix-2/boleto-1/cartão-de-crédito-1).
// Deserialized from snake_case. State-dependent keys are nullable.

/// <summary>Order object (response of <c>POST /orders</c> and <c>GET /orders/{id}</c>).</summary>
public sealed class V5OrderResponse
{
    /// <summary><c>or_XXXXXXXXXXXXXXXX</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? Code { get; set; }
    public long Amount { get; set; }
    public string? Currency { get; set; }
    public bool Closed { get; set; }

    /// <summary>Order status: <c>pending</c> | <c>paid</c> | <c>canceled</c> | <c>failed</c>.</summary>
    public string? Status { get; set; }

    public List<V5OrderItemResponse>? Items { get; set; }
    public V5CustomerResponse? Customer { get; set; }
    public List<V5ChargeResponse>? Charges { get; set; }

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class V5OrderItemResponse
{
    /// <summary><c>oi_XXXXXXXXXXXXXXXX</c>.</summary>
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Description { get; set; }
    public long Amount { get; set; }
    public int Quantity { get; set; }
    public string? Code { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>Charge object (response of <c>GET /charges/{id}</c>, capture, cancel).</summary>
public sealed class V5ChargeResponse
{
    /// <summary><c>ch_XXXXXXXXXXXXXXXX</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? Code { get; set; }
    public string? GatewayId { get; set; }

    public long Amount { get; set; }

    /// <summary>Present when paid/captured.</summary>
    public long? PaidAmount { get; set; }

    /// <summary>Present on credit-card refund/cancel.</summary>
    public long? CanceledAmount { get; set; }

    /// <summary>Charge status: <c>pending</c> | <c>paid</c> | <c>canceled</c> | <c>processing</c> | <c>failed</c> | <c>overpaid</c> | <c>underpaid</c> | <c>chargedback</c>.</summary>
    public string? Status { get; set; }

    public string? Currency { get; set; }

    /// <summary><c>credit_card</c> | <c>debit_card</c> | <c>boleto</c> | <c>pix</c> | <c>voucher</c>.</summary>
    public string? PaymentMethod { get; set; }

    /// <summary>Card charges: <c>credit</c> | <c>debit</c> | <c>prepaid</c>.</summary>
    public string? FundingSource { get; set; }

    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public DateTimeOffset? CanceledAt { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public V5CustomerResponse? Customer { get; set; }
    public V5TransactionResponse? LastTransaction { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// The <c>last_transaction</c> object. Polymorphic by <see cref="TransactionType"/>: this class is
/// the superset of the common fields plus the card / PIX / boleto specific fields.
/// </summary>
public sealed class V5TransactionResponse
{
    // Common fields (all payment methods)
    public string? Id { get; set; }

    /// <summary>Discriminator: <c>credit_card</c> | <c>debit_card</c> | <c>boleto</c> | <c>pix</c> | <c>voucher</c>.</summary>
    public string? TransactionType { get; set; }

    public string? GatewayId { get; set; }
    public long Amount { get; set; }

    /// <summary>Transaction status; the value set differs per payment method (see PagarmeStatusMapper).</summary>
    public string? Status { get; set; }

    public bool Success { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public V5GatewayResponse? GatewayResponse { get; set; }

    // Card fields [credit_card / debit_card]
    public int Installments { get; set; }
    public string? InstallmentType { get; set; }
    public string? FundingSource { get; set; }
    public string? StatementDescriptor { get; set; }
    public string? AcquirerName { get; set; }
    public string? AcquirerTid { get; set; }
    public string? AcquirerNsu { get; set; }
    public string? AcquirerAuthCode { get; set; }
    public string? AcquirerMessage { get; set; }
    public string? AcquirerReturnCode { get; set; }
    public string? EntryMode { get; set; }
    public string? OperationType { get; set; }
    public string? PaymentType { get; set; }
    public V5CardResponse? Card { get; set; }

    // PIX fields [pix]
    public string? QrCode { get; set; }
    public string? QrCodeUrl { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? EndToEndId { get; set; }
    public V5PixPayerResponse? Payer { get; set; }

    // Boleto fields [boleto]
    public string? Url { get; set; }
    public string? Pdf { get; set; }
    public string? Line { get; set; }
    public string? Barcode { get; set; }
    public string? NossoNumero { get; set; }

    /// <summary>Boleto type (<c>DM</c> | <c>BDP</c>) — shares the JSON key with the credit-card field of the same name.</summary>
    public string? Bank { get; set; }

    public string? DocumentNumber { get; set; }
    public string? Instructions { get; set; }
    public DateTimeOffset? DueAt { get; set; }
}

public sealed class V5GatewayResponse
{
    public string? Code { get; set; }
    public List<V5GatewayError>? Errors { get; set; }
}

public sealed class V5GatewayError
{
    public string? Message { get; set; }
}

public sealed class V5CardResponse
{
    /// <summary><c>card_XXXXXXXXXXXXXXXX</c>.</summary>
    public string? Id { get; set; }
    public string? FirstSixDigits { get; set; }
    public string? LastFourDigits { get; set; }
    public string? Brand { get; set; }
    public string? HolderName { get; set; }
    public int ExpMonth { get; set; }
    public int ExpYear { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public sealed class V5PixPayerResponse
{
    public string? Name { get; set; }
    public string? DocumentType { get; set; }

    /// <summary>Masked document, e.g. <c>***.777.888-**</c>.</summary>
    public string? Document { get; set; }

    public V5PixBankAccountResponse? BankAccount { get; set; }
}

public sealed class V5PixBankAccountResponse
{
    public string? BankName { get; set; }
    public string? Ispb { get; set; }
}

public sealed class V5CustomerResponse
{
    /// <summary><c>cus_XXXXXXXXXXXXXXXX</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Code { get; set; }
    public string? Document { get; set; }
    public string? DocumentType { get; set; }
    public string? Type { get; set; }
    public string? Gender { get; set; }
    public bool Delinquent { get; set; }
    public V5AddressResponse? Address { get; set; }
    public V5PhonesResponse? Phones { get; set; }
    public DateTimeOffset? Birthdate { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class V5AddressResponse
{
    /// <summary><c>addr_XXXXXXXXXXXXXXXX</c>.</summary>
    public string? Id { get; set; }

    [JsonPropertyName("line_1")]
    public string? Line1 { get; set; }

    [JsonPropertyName("line_2")]
    public string? Line2 { get; set; }

    public string? ZipCode { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? Status { get; set; }
}

public sealed class V5PhonesResponse
{
    public V5PhoneResponse? HomePhone { get; set; }
    public V5PhoneResponse? MobilePhone { get; set; }
}

public sealed class V5PhoneResponse
{
    public string? CountryCode { get; set; }
    public string? AreaCode { get; set; }
    public string? Number { get; set; }
}
