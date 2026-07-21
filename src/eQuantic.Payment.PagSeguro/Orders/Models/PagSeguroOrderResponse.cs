using System.Text.Json.Serialization;

namespace eQuantic.Payment.PagSeguro.Orders.Models;

// Faithful wire models for the PagSeguro Orders/Charges API responses, mirroring the reference 1:1.
// Money is integer centavos. Card/boleto data live in charge.payment_method; PIX in qr_codes[].

/// <summary>The <c>order</c> object (response of <c>POST /orders</c>, <c>GET /orders/{id}</c>).</summary>
public sealed class PagSeguroOrderResponse
{
    /// <summary><c>ORDE_...</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? ReferenceId { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public PagSeguroCustomer? Customer { get; set; }
    public List<PagSeguroChargeResponse>? Charges { get; set; }
    public List<PagSeguroQrCodeResponse>? QrCodes { get; set; }
    public List<PagSeguroLink>? Links { get; set; }
}

/// <summary>The <c>charge</c> object (response of <c>GET /charges/{id}</c>, capture, cancel).</summary>
public sealed class PagSeguroChargeResponse
{
    /// <summary><c>CHAR_...</c>.</summary>
    public string Id { get; set; } = string.Empty;

    public string? ReferenceId { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string? Description { get; set; }
    public PagSeguroAmountResponse? Amount { get; set; }
    public PagSeguroPaymentResponseInfo? PaymentResponse { get; set; }
    public PagSeguroPaymentMethodResponse? PaymentMethod { get; set; }
    public List<PagSeguroQrCodeResponse>? QrCodes { get; set; }
    public List<PagSeguroLink>? Links { get; set; }
}

public sealed class PagSeguroAmountResponse
{
    public long Value { get; set; }
    public string? Currency { get; set; }
    public PagSeguroAmountSummary? Summary { get; set; }
}

public sealed class PagSeguroAmountSummary
{
    public long Total { get; set; }
    public long Paid { get; set; }
    public long Refunded { get; set; }
}

public sealed class PagSeguroPaymentResponseInfo
{
    /// <summary>PagSeguro returns this as a string or a number; <c>20000</c> = success.</summary>
    [JsonConverter(typeof(FlexibleStringConverter))]
    public string? Code { get; set; }

    public string? Message { get; set; }
    public string? Reference { get; set; }
}

public sealed class PagSeguroPaymentMethodResponse
{
    /// <summary><c>CREDIT_CARD</c> | <c>DEBIT_CARD</c> | <c>BOLETO</c>.</summary>
    public string? Type { get; set; }

    public int? Installments { get; set; }
    public bool? Capture { get; set; }
    public string? SoftDescriptor { get; set; }
    public PagSeguroCardResponse? Card { get; set; }
    public PagSeguroBoletoResponse? Boleto { get; set; }
}

public sealed class PagSeguroCardResponse
{
    public string? Brand { get; set; }
    public string? FirstDigits { get; set; }
    public string? LastDigits { get; set; }
    public string? ExpMonth { get; set; }
    public string? ExpYear { get; set; }
    public PagSeguroCardHolder? Holder { get; set; }
}

public sealed class PagSeguroBoletoResponse
{
    /// <summary>Boleto id (raw UUID).</summary>
    public string? Id { get; set; }

    public string? Barcode { get; set; }
    public string? FormattedBarcode { get; set; }
    public string? DueDate { get; set; }
    public PagSeguroInstructionLines? InstructionLines { get; set; }
}

public sealed class PagSeguroQrCodeResponse
{
    /// <summary><c>QRCO_...</c>.</summary>
    public string? Id { get; set; }

    /// <summary>PIX "copia e cola" (EMV BR Code payload).</summary>
    public string? Text { get; set; }

    public PagSeguroAmountResponse? Amount { get; set; }
    public DateTimeOffset? ExpirationDate { get; set; }
    public List<PagSeguroLink>? Links { get; set; }
}

public sealed class PagSeguroLink
{
    /// <summary><c>SELF</c>, <c>PAY</c>, <c>CHARGE.CAPTURE</c>, <c>CHARGE.CANCEL</c>, <c>QRCODE.PNG</c>, <c>QRCODE.BASE64</c>.</summary>
    public string? Rel { get; set; }

    public string? Href { get; set; }
    public string? Media { get; set; }
    public string? Type { get; set; }
}
