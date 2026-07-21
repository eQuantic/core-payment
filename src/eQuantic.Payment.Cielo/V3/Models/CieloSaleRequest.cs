namespace eQuantic.Payment.Cielo.V3.Models;

// Faithful wire models for the Cielo API 3.0 create-sale request (POST /1/sales/), mirroring the reference 1:1.
// Serialized as PascalCase (no naming policy); null omitted. Money is integer centavos in Payment.Amount.

/// <summary>Request body for <c>POST /1/sales/</c>.</summary>
public sealed class CieloSaleRequest
{
    /// <summary>Merchant order id (alphanumeric, no special characters, max 50).</summary>
    public string MerchantOrderId { get; set; } = string.Empty;

    /// <summary>Buyer data. Cielo embeds the customer inline with the sale (no standalone resource).</summary>
    public CieloCustomerRequest? Customer { get; set; }

    /// <summary>Payment details (method, amount, card/PIX/boleto).</summary>
    public CieloPaymentRequest Payment { get; set; } = new();
}

/// <summary>Buyer block of the sale request.</summary>
public sealed class CieloCustomerRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }

    /// <summary>CPF/CNPJ digits.</summary>
    public string? Identity { get; set; }

    /// <summary><c>CPF</c> | <c>CNPJ</c>.</summary>
    public string? IdentityType { get; set; }
}

/// <summary>Payment block of the sale request. Boleto fields are flat on this object (no sub-object).</summary>
public sealed class CieloPaymentRequest
{
    /// <summary><c>CreditCard</c> | <c>DebitCard</c> | <c>Pix</c> | <c>Boleto</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Amount in centavos.</summary>
    public long Amount { get; set; }

    /// <summary>Acquirer/bank provider; required for boleto/debit (e.g. <c>Bradesco2</c>, <c>Simulado</c>).</summary>
    public string? Provider { get; set; }

    /// <summary>Number of installments (card only); <c>1</c> = à vista.</summary>
    public int? Installments { get; set; }

    /// <summary><c>true</c> auto-captures; <c>false</c> pre-authorizes (credit card).</summary>
    public bool? Capture { get; set; }

    /// <summary>Text on the cardholder statement (max 13 characters).</summary>
    public string? SoftDescriptor { get; set; }

    /// <summary>Credit card data (when <see cref="Type"/> is <c>CreditCard</c>).</summary>
    public CieloCardRequest? CreditCard { get; set; }

    /// <summary>Debit card data (when <see cref="Type"/> is <c>DebitCard</c>).</summary>
    public CieloCardRequest? DebitCard { get; set; }

    /// <summary>Boleto due date, format <c>YYYY-MM-DD</c> (boleto only).</summary>
    public string? ExpirationDate { get; set; }

    /// <summary>Boleto instructions (boleto only).</summary>
    public string? Instructions { get; set; }
}

/// <summary>Card data for a credit or debit sale.</summary>
public sealed class CieloCardRequest
{
    /// <summary>PAN.</summary>
    public string? CardNumber { get; set; }

    /// <summary>Cardholder name (no accents/special characters).</summary>
    public string? Holder { get; set; }

    /// <summary>Expiration, format <c>MM/YYYY</c>.</summary>
    public string? ExpirationDate { get; set; }

    /// <summary>CVV.</summary>
    public string? SecurityCode { get; set; }

    /// <summary>Card brand: <c>Visa</c>/<c>Master</c>/<c>Amex</c>/<c>Elo</c>/<c>Hipercard</c>/… .</summary>
    public string? Brand { get; set; }

    /// <summary>Stored/tokenized card id (used instead of the raw PAN fields).</summary>
    public string? CardToken { get; set; }
}
