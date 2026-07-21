namespace eQuantic.Payment.Pagarme.V4.Models;

// Faithful wire models for the legacy Pagar.me API (/1), mirroring the official reference 1:1
// (docs.pagar.me/v1/reference/objeto-transaction and related pages). Transaction-based.
// Serialized/deserialized as snake_case; null properties are omitted on the wire.
// Auth: api_key travels in the request body (POST) or query string (GET), never a header.

// ── Requests ────────────────────────────────────────────────────────────────

/// <summary>Request body for <c>POST /transactions</c>.</summary>
public sealed class V4TransactionRequest
{
    public string? ApiKey { get; set; }

    /// <summary>Value in centavos (minimum 100).</summary>
    public long Amount { get; set; }

    /// <summary><c>credit_card</c> | <c>boleto</c> | <c>pix</c> (some accounts also expose <c>debit_card</c>).</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    // Card — provide exactly one of: card_hash | card_id | the raw quartet below.
    public string? CardHash { get; set; }
    public string? CardId { get; set; }
    public string? CardNumber { get; set; }
    public string? CardHolderName { get; set; }

    /// <summary>Format <c>MMYY</c> (e.g. <c>1225</c>).</summary>
    public string? CardExpirationDate { get; set; }
    public string? CardCvv { get; set; }

    /// <summary>1–12.</summary>
    public int? Installments { get; set; }

    /// <summary><c>false</c> authorizes only (capture later within 5 days).</summary>
    public bool? Capture { get; set; }

    /// <summary>When <c>true</c>, returns <c>processing</c> immediately and notifies via <see cref="PostbackUrl"/>.</summary>
    public bool? Async { get; set; }

    public string? PostbackUrl { get; set; }

    /// <summary>Max 13 alphanumeric chars (statement descriptor).</summary>
    public string? SoftDescriptor { get; set; }

    public string? ReferenceKey { get; set; }

    // Boleto
    public DateTimeOffset? BoletoExpirationDate { get; set; }
    public string? BoletoInstructions { get; set; }
    public V4BoletoFineOrInterest? BoletoFine { get; set; }
    public V4BoletoFineOrInterest? BoletoInterest { get; set; }
    public List<string>? BoletoRules { get; set; }

    // PIX
    public DateTimeOffset? PixExpirationDate { get; set; }
    public List<V4PixAdditionalField>? PixAdditionalFields { get; set; }

    public V4Customer? Customer { get; set; }
    public V4Billing? Billing { get; set; }
    public V4Shipping? Shipping { get; set; }
    public List<V4Item>? Items { get; set; }
    public List<V4SplitRule>? SplitRules { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class V4BoletoFineOrInterest
{
    public int? Days { get; set; }
    public long? Amount { get; set; }
    public decimal? Percentage { get; set; }
}

public sealed class V4PixAdditionalField
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

/// <summary>Request body for <c>POST /transactions/{id}/capture</c>.</summary>
public sealed class V4CaptureRequest
{
    public string? ApiKey { get; set; }
    public long? Amount { get; set; }
    public List<V4SplitRule>? SplitRules { get; set; }
}

/// <summary>Request body for <c>POST /transactions/{id}/refund</c>.</summary>
public sealed class V4RefundRequest
{
    public string? ApiKey { get; set; }
    public long? Amount { get; set; }
    public bool? Async { get; set; }
    public string? BankAccountId { get; set; }
    public V4BankAccount? BankAccount { get; set; }
}

public sealed class V4BankAccount
{
    public string? BankCode { get; set; }
    public string? Agencia { get; set; }
    public string? AgenciaDv { get; set; }
    public string? Conta { get; set; }
    public string? ContaDv { get; set; }
    public string? DocumentNumber { get; set; }
    public string? LegalName { get; set; }
    public string? Type { get; set; }
}

/// <summary>Request body for <c>POST /customers</c> (v4 model).</summary>
public sealed class V4CustomerRequest
{
    public string? ApiKey { get; set; }
    public string? ExternalId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }

    /// <summary><c>individual</c> | <c>corporation</c>.</summary>
    public string? Type { get; set; }

    /// <summary>ISO 3166-1 alpha-2, lowercase (e.g. <c>br</c>).</summary>
    public string Country { get; set; } = "br";

    public List<V4Document>? Documents { get; set; }

    /// <summary>E.164 numbers (e.g. <c>+5511999998888</c>).</summary>
    public List<string>? PhoneNumbers { get; set; }

    /// <summary>Format <c>YYYY-MM-DD</c>.</summary>
    public string? Birthday { get; set; }
}

// ── Shared nested objects ─────────────────────────────────────────────────────

public sealed class V4Customer
{
    public string? Object { get; set; }
    public long? Id { get; set; }
    public string? ExternalId { get; set; }

    /// <summary><c>individual</c> | <c>corporation</c>.</summary>
    public string? Type { get; set; }

    public string? Country { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }

    /// <summary>Legacy flat document (may co-exist with <see cref="Documents"/>).</summary>
    public string? DocumentNumber { get; set; }
    public string? DocumentType { get; set; }

    public List<string>? PhoneNumbers { get; set; }
    public List<V4Document>? Documents { get; set; }

    public string? Birthday { get; set; }
    public string? BornAt { get; set; }
    public string? Gender { get; set; }
}

public sealed class V4Document
{
    public string? Object { get; set; }
    public long? Id { get; set; }

    /// <summary><c>cpf</c> | <c>cnpj</c> | <c>passport</c>.</summary>
    public string Type { get; set; } = "cpf";

    public string Number { get; set; } = string.Empty;
}

public sealed class V4Phone
{
    public string? Object { get; set; }
    public long? Id { get; set; }
    public string? Ddd { get; set; }
    public string? Number { get; set; }
}

public sealed class V4Address
{
    public string? Object { get; set; }
    public long? Id { get; set; }
    public string? Street { get; set; }
    public string? StreetNumber { get; set; }
    public string? Complementary { get; set; }
    public string? Neighborhood { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zipcode { get; set; }
    public string? Country { get; set; }
}

public sealed class V4Billing
{
    public string? Object { get; set; }
    public long? Id { get; set; }
    public string? Name { get; set; }
    public V4Address? Address { get; set; }
}

public sealed class V4Shipping
{
    public string? Object { get; set; }
    public long? Id { get; set; }
    public string? Name { get; set; }
    public long? Fee { get; set; }
    public string? DeliveryDate { get; set; }
    public bool? Expedited { get; set; }
    public V4Address? Address { get; set; }
}

public sealed class V4Item
{
    public string? Object { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public long UnitPrice { get; set; }
    public int Quantity { get; set; }
    public bool Tangible { get; set; }
    public string? Category { get; set; }
}

public sealed class V4SplitRule
{
    public string? Object { get; set; }
    public long? Id { get; set; }
    public string RecipientId { get; set; } = string.Empty;
    public bool? ChargeProcessingFee { get; set; }
    public bool? Liable { get; set; }
    public decimal? Percentage { get; set; }
    public long? Amount { get; set; }
    public bool? ChargeRemainder { get; set; }
}

public sealed class V4Card
{
    public string? Object { get; set; }
    public string? Id { get; set; }
    public DateTimeOffset? DateCreated { get; set; }
    public DateTimeOffset? DateUpdated { get; set; }
    public string? Brand { get; set; }
    public string? HolderName { get; set; }
    public string? FirstDigits { get; set; }
    public string? LastDigits { get; set; }
    public string? Country { get; set; }
    public string? Fingerprint { get; set; }
    public bool? Valid { get; set; }

    /// <summary>Format <c>MMYY</c>.</summary>
    public string? ExpirationDate { get; set; }
}

// ── Transaction response ──────────────────────────────────────────────────────

/// <summary>The <c>transaction</c> object (response of create, get, capture, refund).</summary>
public sealed class V4TransactionResponse
{
    public string? Object { get; set; }

    /// <summary>Numeric transaction id in the legacy <c>/1</c> API.</summary>
    public long Id { get; set; }

    /// <summary>See <see cref="PagarmeStatusMapper.FromV4"/> for the exhaustive value set.</summary>
    public string? Status { get; set; }

    /// <summary><c>acquirer</c> | <c>antifraud</c> | <c>internal_error</c> | <c>no_acquirer</c> | <c>acquirer_timeout</c>.</summary>
    public string? RefuseReason { get; set; }
    public string? StatusReason { get; set; }

    public string? AcquirerResponseCode { get; set; }
    public string? AcquirerName { get; set; }
    public string? AcquirerId { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? SoftDescriptor { get; set; }
    public string? Tid { get; set; }
    public string? Nsu { get; set; }

    public DateTimeOffset? DateCreated { get; set; }
    public DateTimeOffset? DateUpdated { get; set; }

    public long Amount { get; set; }
    public long? AuthorizedAmount { get; set; }
    public long? PaidAmount { get; set; }
    public long? RefundedAmount { get; set; }
    public int? Installments { get; set; }
    public decimal? Cost { get; set; }

    public string? PostbackUrl { get; set; }
    public string? PaymentMethod { get; set; }
    public string? CaptureMethod { get; set; }
    public string? AntifraudScore { get; set; }
    public string? Referer { get; set; }
    public string? Ip { get; set; }
    public string? ReferenceKey { get; set; }
    public long? SubscriptionId { get; set; }

    // Card (flattened, credit_card only)
    public string? CardHolderName { get; set; }
    public string? CardLastDigits { get; set; }
    public string? CardFirstDigits { get; set; }
    public string? CardBrand { get; set; }
    public string? CardPinMode { get; set; }

    // Boleto (populated after authorization/capture)
    public string? BoletoUrl { get; set; }
    public string? BoletoBarcode { get; set; }
    public DateTimeOffset? BoletoExpirationDate { get; set; }

    // PIX
    public string? PixQrCode { get; set; }
    public DateTimeOffset? PixExpirationDate { get; set; }
    public List<V4PixAdditionalField>? PixAdditionalFields { get; set; }

    // Nested objects/arrays
    public V4Phone? Phone { get; set; }
    public V4Address? Address { get; set; }
    public V4Customer? Customer { get; set; }
    public V4Billing? Billing { get; set; }
    public V4Shipping? Shipping { get; set; }
    public List<V4Item>? Items { get; set; }
    public V4Card? Card { get; set; }
    public List<V4SplitRule>? SplitRules { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public Dictionary<string, string>? AntifraudMetadata { get; set; }
}

// ── Customer response ─────────────────────────────────────────────────────────

/// <summary>The <c>customer</c> object (response of create/get customer).</summary>
public sealed class V4CustomerResponse
{
    public string? Object { get; set; }
    public long Id { get; set; }
    public string? ExternalId { get; set; }
    public string? Type { get; set; }
    public string? Country { get; set; }
    public string? DocumentNumber { get; set; }
    public string? DocumentType { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public List<string>? PhoneNumbers { get; set; }
    public List<V4Document>? Documents { get; set; }
    public string? BornAt { get; set; }
    public string? Birthday { get; set; }
    public string? Gender { get; set; }
    public DateTimeOffset? DateCreated { get; set; }
}
