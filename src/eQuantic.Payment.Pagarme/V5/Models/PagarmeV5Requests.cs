using System.Text.Json.Serialization;

namespace eQuantic.Payment.Pagarme.V5.Models;

// Faithful wire models for Pagar.me Core API v5 requests, mirroring the official
// reference 1:1 (https://docs.pagar.me/reference/criar-pedido-2). Serialized as snake_case.
// null properties are omitted on the wire (see PagarmeClientV5 serializer options).

/// <summary>Request body for <c>POST /orders</c>.</summary>
public sealed class V5OrderRequest
{
    /// <summary>Merchant order identifier (max 52 chars).</summary>
    public string? Code { get; set; }

    public List<V5OrderItemRequest> Items { get; set; } = [];

    /// <summary>Existing customer id (<c>cus_...</c>); mutually exclusive with <see cref="Customer"/>.</summary>
    public string? CustomerId { get; set; }

    public V5CustomerRequest? Customer { get; set; }

    public List<V5PaymentRequest> Payments { get; set; } = [];

    public V5ShippingRequest? Shipping { get; set; }

    /// <summary><c>true</c> (default) closes the order immediately; <c>false</c> keeps it editable.</summary>
    public bool Closed { get; set; } = true;

    /// <summary>Transaction origin channel (e.g. <c>payment_link</c>).</summary>
    public string? Channel { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class V5OrderItemRequest
{
    /// <summary>Item code in the merchant system.</summary>
    public string? Code { get; set; }

    /// <summary>Unit value in centavos (must be &gt; 0).</summary>
    public long Amount { get; set; }

    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public string? Category { get; set; }
}

public sealed class V5PaymentRequest
{
    /// <summary><c>credit_card</c> | <c>debit_card</c> | <c>boleto</c> | <c>pix</c> | <c>voucher</c>.</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    /// <summary>Amount for this payment (used on multi-payment orders).</summary>
    public long? Amount { get; set; }

    public V5CreditCardPaymentRequest? CreditCard { get; set; }
    public V5DebitCardPaymentRequest? DebitCard { get; set; }
    public V5PixPaymentRequest? Pix { get; set; }
    public V5BoletoPaymentRequest? Boleto { get; set; }
}

public sealed class V5CreditCardPaymentRequest
{
    public int Installments { get; set; } = 1;

    public string? StatementDescriptor { get; set; }

    /// <summary><c>auth_and_capture</c> | <c>auth_only</c> | <c>pre_auth</c>.</summary>
    public string? OperationType { get; set; }

    public V5CardRequest? Card { get; set; }

    /// <summary>Previously stored card id.</summary>
    public string? CardId { get; set; }

    /// <summary>Tokenized card (Gateway only).</summary>
    public string? CardToken { get; set; }

    /// <summary><c>first</c> | <c>subsequent</c>.</summary>
    public string? RecurrenceCycle { get; set; }
}

public sealed class V5DebitCardPaymentRequest
{
    public string? StatementDescriptor { get; set; }
    public V5CardRequest? Card { get; set; }
    public string? CardId { get; set; }
    public string? CardToken { get; set; }
    public V5CardAuthenticationRequest? Authentication { get; set; }
}

public sealed class V5CardRequest
{
    /// <summary>13–19 digits.</summary>
    public string? Number { get; set; }

    public string? HolderName { get; set; }

    /// <summary>1–12.</summary>
    public int ExpMonth { get; set; }

    /// <summary><c>yy</c> or <c>yyyy</c>.</summary>
    public int ExpYear { get; set; }

    public string? Cvv { get; set; }

    /// <summary>Required for voucher (VR/Pluxee).</summary>
    public string? HolderDocument { get; set; }

    public string? Brand { get; set; }

    public V5BillingAddressRequest? BillingAddress { get; set; }
}

public sealed class V5CardAuthenticationRequest
{
    /// <summary><c>threed_secure</c>.</summary>
    public string Type { get; set; } = "threed_secure";
    public V5ThreeDSecureRequest? ThreedSecure { get; set; }
}

public sealed class V5ThreeDSecureRequest
{
    /// <summary><c>pagarme</c> | <c>third_party</c>.</summary>
    public string? Mpi { get; set; }
    public string? TransactionId { get; set; }
    public string? Eci { get; set; }
    public string? Cavv { get; set; }
    public string? DsTransactionId { get; set; }
    public string? Version { get; set; }
}

public sealed class V5PixPaymentRequest
{
    /// <summary>Expiration in seconds (mutually complementary with <see cref="ExpiresAt"/>).</summary>
    public int? ExpiresIn { get; set; }

    /// <summary>Absolute expiry (UTC).</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Extra info shown to the payer.</summary>
    public List<V5PixAdditionalInformation>? AdditionalInformation { get; set; }
}

public sealed class V5PixAdditionalInformation
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class V5BoletoPaymentRequest
{
    /// <summary>Bank code: <c>001</c> BB, <c>033</c> Santander, <c>237</c> Bradesco, <c>341</c> Itaú, <c>104</c> CEF.</summary>
    public string? Bank { get; set; }

    /// <summary>Instructions (max 256 chars).</summary>
    public string? Instructions { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public string? NossoNumero { get; set; }

    /// <summary><c>DM</c> (Duplicata Mercantil) | <c>BDP</c> (Boleto de Proposta).</summary>
    public string? Type { get; set; }

    public string? DocumentNumber { get; set; }
}

public sealed class V5CustomerRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }

    /// <summary>Merchant reference (max 52 chars).</summary>
    public string? Code { get; set; }

    public string? Document { get; set; }

    /// <summary><c>CPF</c> | <c>CNPJ</c> | <c>PASSPORT</c>.</summary>
    public string? DocumentType { get; set; }

    /// <summary><c>individual</c> | <c>company</c>; required when <see cref="Document"/> is sent.</summary>
    public string? Type { get; set; }

    /// <summary><c>male</c> | <c>female</c>.</summary>
    public string? Gender { get; set; }

    public V5AddressRequest? Address { get; set; }
    public V5PhonesRequest? Phones { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class V5AddressRequest
{
    /// <summary>"Number, Street, Neighborhood".</summary>
    [JsonPropertyName("line_1")]
    public string Line1 { get; set; } = string.Empty;

    [JsonPropertyName("line_2")]
    public string? Line2 { get; set; }

    public string ZipCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    /// <summary>ISO 3166-1 alpha-2.</summary>
    public string Country { get; set; } = "BR";
}

/// <summary>Card billing address (same shape as <see cref="V5AddressRequest"/>).</summary>
public sealed class V5BillingAddressRequest
{
    [JsonPropertyName("line_1")]
    public string Line1 { get; set; } = string.Empty;

    [JsonPropertyName("line_2")]
    public string? Line2 { get; set; }

    public string ZipCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = "BR";
}

public sealed class V5PhonesRequest
{
    public V5PhoneRequest? HomePhone { get; set; }
    public V5PhoneRequest? MobilePhone { get; set; }
}

public sealed class V5PhoneRequest
{
    public string CountryCode { get; set; } = "55";
    public string AreaCode { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
}

public sealed class V5ShippingRequest
{
    public long Amount { get; set; }
    public string? Description { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public V5AddressRequest? Address { get; set; }
}

/// <summary>Request body for <c>POST /charges/{id}/capture</c>.</summary>
public sealed class V5CaptureRequest
{
    public long? Amount { get; set; }
    public string? Code { get; set; }
}

/// <summary>Request body for <c>DELETE /charges/{id}</c> (cancel / refund).</summary>
public sealed class V5CancelRequest
{
    public long? Amount { get; set; }
}
