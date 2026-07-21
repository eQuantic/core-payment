namespace eQuantic.Payment.PagSeguro.Orders.Models;

// Faithful wire models for the PagSeguro Orders/Charges API request (POST /orders), mirroring the reference 1:1.
// Serialized as snake_case; null omitted. Money is integer centavos in amount.value.

/// <summary>Request body for <c>POST /orders</c>. Card/boleto go in <see cref="Charges"/>; PIX in <see cref="QrCodes"/>.</summary>
public sealed class PagSeguroOrderRequest
{
    public string? ReferenceId { get; set; }
    public PagSeguroCustomer Customer { get; set; } = new();
    public List<PagSeguroItem> Items { get; set; } = [];
    public List<PagSeguroChargeRequest>? Charges { get; set; }
    public List<PagSeguroQrCodeRequest>? QrCodes { get; set; }
    public List<string>? NotificationUrls { get; set; }
}

public sealed class PagSeguroCustomer
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>CPF (11) or CNPJ (14), digits only.</summary>
    public string? TaxId { get; set; }

    public List<PagSeguroPhone>? Phones { get; set; }
}

public sealed class PagSeguroPhone
{
    public string? Country { get; set; }
    public string? Area { get; set; }
    public string? Number { get; set; }

    /// <summary><c>MOBILE</c> | <c>BUSINESS</c> | <c>HOME</c>.</summary>
    public string? Type { get; set; }
}

public sealed class PagSeguroItem
{
    public string? ReferenceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;

    /// <summary>Unit value in centavos.</summary>
    public long UnitAmount { get; set; }
}

public sealed class PagSeguroChargeRequest
{
    public string? ReferenceId { get; set; }
    public string? Description { get; set; }
    public PagSeguroAmountRequest Amount { get; set; } = new();
    public PagSeguroPaymentMethodRequest PaymentMethod { get; set; } = new();
}

public sealed class PagSeguroAmountRequest
{
    /// <summary>Value in centavos.</summary>
    public long Value { get; set; }
    public string Currency { get; set; } = "BRL";
}

public sealed class PagSeguroPaymentMethodRequest
{
    /// <summary><c>CREDIT_CARD</c> | <c>DEBIT_CARD</c> | <c>BOLETO</c>.</summary>
    public string Type { get; set; } = string.Empty;

    public int? Installments { get; set; }

    /// <summary><c>true</c> auto-captures; <c>false</c> pre-authorizes (card).</summary>
    public bool? Capture { get; set; }

    public string? SoftDescriptor { get; set; }
    public PagSeguroCardRequest? Card { get; set; }
    public PagSeguroBoletoRequest? Boleto { get; set; }
}

public sealed class PagSeguroCardRequest
{
    /// <summary>Card data encrypted client-side with the PagBank public key (preferred over raw PAN).</summary>
    public string? Encrypted { get; set; }

    public string? Number { get; set; }

    /// <summary>Format <c>MM</c>.</summary>
    public string? ExpMonth { get; set; }

    /// <summary>Format <c>YYYY</c>.</summary>
    public string? ExpYear { get; set; }

    public string? SecurityCode { get; set; }

    /// <summary>Stored card id (reuse a tokenized card).</summary>
    public string? Id { get; set; }

    public bool? Store { get; set; }
    public PagSeguroCardHolder? Holder { get; set; }
}

public sealed class PagSeguroCardHolder
{
    public string? Name { get; set; }
    public string? TaxId { get; set; }
}

public sealed class PagSeguroBoletoRequest
{
    /// <summary>Format <c>YYYY-MM-DD</c>.</summary>
    public string DueDate { get; set; } = string.Empty;

    public PagSeguroBoletoHolder Holder { get; set; } = new();
    public PagSeguroInstructionLines? InstructionLines { get; set; }
}

public sealed class PagSeguroBoletoHolder
{
    public string Name { get; set; } = string.Empty;
    public string? TaxId { get; set; }
    public string? Email { get; set; }
    public PagSeguroBoletoAddress? Address { get; set; }
}

public sealed class PagSeguroBoletoAddress
{
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Locality { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? RegionCode { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}

public sealed class PagSeguroInstructionLines
{
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
}

public sealed class PagSeguroQrCodeRequest
{
    public PagSeguroAmountRequest Amount { get; set; } = new();

    /// <summary>ISO 8601 with offset. Defaults to end of the next day when omitted.</summary>
    public DateTimeOffset? ExpirationDate { get; set; }
}

/// <summary>Request body for capture / cancel (<c>{ "amount": { "value": N } }</c>; omit for full).</summary>
public sealed class PagSeguroAmountEnvelope
{
    public PagSeguroAmountRequest Amount { get; set; } = new();
}
