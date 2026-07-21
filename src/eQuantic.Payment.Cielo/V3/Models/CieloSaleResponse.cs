namespace eQuantic.Payment.Cielo.V3.Models;

// Faithful wire models for the Cielo API 3.0 responses, mirroring the reference 1:1.
// PascalCase keys; money is integer centavos. Card/PIX/boleto artifacts live on the Payment object.
// The create/get sale response nests everything under Payment; capture/void return the flat CieloReturnResponse.

/// <summary>The sale object (response of <c>POST /1/sales/</c> and <c>GET /1/sales/{paymentId}</c>).</summary>
public sealed class CieloSaleResponse
{
    public string? MerchantOrderId { get; set; }
    public CieloCustomerResponse? Customer { get; set; }
    public CieloPaymentResponse? Payment { get; set; }
}

/// <summary>Buyer block echoed on the sale response.</summary>
public sealed class CieloCustomerResponse
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Identity { get; set; }
    public string? IdentityType { get; set; }
}

/// <summary>Payment block of the sale response. Card/PIX/boleto output fields are flattened here.</summary>
public sealed class CieloPaymentResponse
{
    /// <summary>Payment id (GUID); key for all future operations.</summary>
    public string? PaymentId { get; set; }

    /// <summary><c>CreditCard</c> | <c>DebitCard</c> | <c>Pix</c> | <c>Boleto</c>.</summary>
    public string? Type { get; set; }

    /// <summary>Amount in centavos.</summary>
    public long Amount { get; set; }

    /// <summary>Captured amount in centavos (present after capture).</summary>
    public long? CapturedAmount { get; set; }

    public string? Currency { get; set; }
    public int? Installments { get; set; }

    /// <summary>Acquirer transaction id.</summary>
    public string? Tid { get; set; }

    /// <summary>NSU.</summary>
    public string? ProofOfSale { get; set; }

    public string? AuthorizationCode { get; set; }
    public string? SoftDescriptor { get; set; }

    /// <summary>Numeric status (see <c>CieloStatusMapper</c>).</summary>
    public int Status { get; set; }

    /// <summary>Acquirer return code.</summary>
    public string? ReturnCode { get; set; }

    /// <summary>Acquirer return message.</summary>
    public string? ReturnMessage { get; set; }

    public string? ProviderReturnCode { get; set; }
    public string? ProviderReturnMessage { get; set; }

    /// <summary>Format <c>YYYY-MM-DD HH:mm:ss</c>.</summary>
    public string? ReceivedDate { get; set; }

    /// <summary>Format <c>YYYY-MM-DD HH:mm:ss</c>.</summary>
    public string? CapturedDate { get; set; }

    public CieloCardResponse? CreditCard { get; set; }
    public CieloCardResponse? DebitCard { get; set; }

    // PIX
    /// <summary>PIX copy-and-paste ("copia e cola") EMV payload.</summary>
    public string? QrCodeString { get; set; }

    /// <summary>Base64-encoded PNG of the PIX QR code.</summary>
    public string? QrCodeBase64Image { get; set; }

    // Boleto
    /// <summary>Boleto barcode digits.</summary>
    public string? BarCodeNumber { get; set; }

    /// <summary>Boleto digitable line (linha digitável).</summary>
    public string? DigitableLine { get; set; }

    /// <summary>Rendered boleto URL (PDF/HTML).</summary>
    public string? Url { get; set; }

    /// <summary>Boleto expiration, format <c>YYYY-MM-DD</c>.</summary>
    public string? ExpirationDate { get; set; }

    /// <summary>Boleto "nosso número".</summary>
    public string? BoletoNumber { get; set; }

    /// <summary>Boleto assignor (cedente).</summary>
    public string? Assignor { get; set; }

    public List<CieloLink>? Links { get; set; }
}

/// <summary>Card block of the sale response (masked PAN, brand, token).</summary>
public sealed class CieloCardResponse
{
    /// <summary>Masked PAN (e.g. <c>455187******0183</c>).</summary>
    public string? CardNumber { get; set; }

    public string? Holder { get; set; }
    public string? ExpirationDate { get; set; }
    public string? Brand { get; set; }
    public bool? SaveCard { get; set; }

    /// <summary>Token returned when <c>SaveCard</c> was requested.</summary>
    public string? CardToken { get; set; }

    /// <summary>Payment Account Reference (PAR).</summary>
    public string? PaymentAccountReference { get; set; }
}

/// <summary>HATEOAS link entry.</summary>
public sealed class CieloLink
{
    public string? Method { get; set; }
    public string? Rel { get; set; }
    public string? Href { get; set; }
}

/// <summary>
/// Flat response of capture (<c>PUT /1/sales/{id}/capture</c>) and void (<c>PUT /1/sales/{id}/void</c>).
/// Unlike the sale response, the fields are at the root (no <c>Payment</c> wrapper) and no id/amount is echoed.
/// </summary>
public sealed class CieloReturnResponse
{
    /// <summary>Numeric status after the operation (e.g. <c>2</c> captured, <c>10</c> voided, <c>11</c> refunded).</summary>
    public int Status { get; set; }

    public string? ReturnCode { get; set; }
    public string? ReturnMessage { get; set; }
    public int? ReasonCode { get; set; }
    public string? ReasonMessage { get; set; }
    public string? ProviderReturnCode { get; set; }
    public string? ProviderReturnMessage { get; set; }
    public string? Tid { get; set; }
    public string? ProofOfSale { get; set; }
    public string? AuthorizationCode { get; set; }
    public List<CieloLink>? Links { get; set; }
}

/// <summary>A single Cielo validation/business error (<c>[{ "Code": int, "Message": string }]</c>).</summary>
public sealed class CieloError
{
    public int Code { get; set; }
    public string? Message { get; set; }
}
