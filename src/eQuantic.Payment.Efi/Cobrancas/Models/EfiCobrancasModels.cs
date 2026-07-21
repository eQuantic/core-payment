namespace eQuantic.Payment.Efi.Cobrancas.Models;

// Faithful wire models for the Efí Cobranças API (snake_case, English keys). Money is INTEGER centavos.

/// <summary>Request body for <c>POST /v1/charge/one-step</c>.</summary>
public sealed class EfiOneStepRequest
{
    public List<EfiItem> Items { get; set; } = [];
    public EfiMetadata? Metadata { get; set; }
    public EfiPaymentRequest Payment { get; set; } = new();
}

public sealed class EfiItem
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Unit value in centavos.</summary>
    public long Value { get; set; }

    public int Amount { get; set; } = 1;
}

public sealed class EfiMetadata
{
    public string? CustomId { get; set; }
    public string? NotificationUrl { get; set; }
}

public sealed class EfiPaymentRequest
{
    public EfiBankingBillet? BankingBillet { get; set; }
    public EfiCreditCard? CreditCard { get; set; }
}

public sealed class EfiBankingBillet
{
    /// <summary>Format <c>YYYY-MM-DD</c>.</summary>
    public string ExpireAt { get; set; } = string.Empty;

    public EfiCobrancasCustomer Customer { get; set; } = new();
    public string? Message { get; set; }
}

public sealed class EfiCreditCard
{
    /// <summary>Card token generated client-side by the Efí JS library.</summary>
    public string PaymentToken { get; set; } = string.Empty;

    public int Installments { get; set; } = 1;
    public EfiCobrancasCustomer Customer { get; set; } = new();
    public EfiCobrancasAddress? BillingAddress { get; set; }
}

public sealed class EfiCobrancasCustomer
{
    public string? Name { get; set; }
    public string? Cpf { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public EfiJuridicalPerson? JuridicalPerson { get; set; }
    public EfiCobrancasAddress? Address { get; set; }
}

public sealed class EfiJuridicalPerson
{
    public string? CorporateName { get; set; }
    public string? Cnpj { get; set; }
}

public sealed class EfiCobrancasAddress
{
    public string? Street { get; set; }
    public string? Number { get; set; }
    public string? Neighborhood { get; set; }
    public string? Zipcode { get; set; }
    public string? City { get; set; }
    public string? Complement { get; set; }
    public string? State { get; set; }
}

/// <summary>Envelope for Cobranças responses (<c>{ code, data }</c>).</summary>
public sealed class EfiOneStepResponse
{
    public int Code { get; set; }
    public EfiChargeData? Data { get; set; }
}

public sealed class EfiChargeData
{
    /// <summary>Charge id (integer; occasionally a string in card responses — read tolerantly).</summary>
    public long ChargeId { get; set; }

    public string? Status { get; set; }

    /// <summary>Total in centavos.</summary>
    public long Total { get; set; }

    /// <summary>Method name: <c>banking_billet</c> | <c>credit_card</c>.</summary>
    public string? Payment { get; set; }

    public string? Barcode { get; set; }
    public EfiPixOnBoleto? Pix { get; set; }
    public string? Link { get; set; }
    public string? BilletLink { get; set; }
    public EfiPdf? Pdf { get; set; }
    public int? Installments { get; set; }
    public long? InstallmentValue { get; set; }
    public string? ExpireAt { get; set; }
    public string? CreatedAt { get; set; }
}

public sealed class EfiPixOnBoleto
{
    public string? Qrcode { get; set; }
    public string? QrcodeImage { get; set; }
}

public sealed class EfiPdf
{
    public string? Charge { get; set; }
}
