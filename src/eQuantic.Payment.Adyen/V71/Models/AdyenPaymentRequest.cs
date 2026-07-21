namespace eQuantic.Payment.Adyen.V71.Models;

// Faithful wire models for POST /v71/payments, mirroring the reference 1:1.
// Serialized as camelCase; nulls omitted. Money is an integer in minor units in amount.value.

/// <summary>Request body for <c>POST /v71/payments</c>.</summary>
public sealed class AdyenPaymentRequest
{
    public AdyenAmount Amount { get; set; } = new();

    /// <summary>Polymorphic on <see cref="AdyenPaymentMethodRequest.Type"/> (<c>scheme</c> | <c>pix</c> | <c>boletobancario</c>).</summary>
    public AdyenPaymentMethodRequest PaymentMethod { get; set; } = new();

    /// <summary>Adyen merchant account name (set by the operations layer from options, not part of the unified request).</summary>
    public string MerchantAccount { get; set; } = string.Empty;

    /// <summary>Your unique order reference.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Required for redirect/action methods (3DS cards, boleto, PIX). A placeholder is sent when none applies.</summary>
    public string? ReturnUrl { get; set; }

    /// <summary>Shopper email (recommended for boleto/PIX voucher delivery).</summary>
    public string? ShopperEmail { get; set; }

    /// <summary>Brazil CPF (11 digits) or CNPJ (14 digits). Required for boleto, recommended for PIX.</summary>
    public string? SocialSecurityNumber { get; set; }

    /// <summary>Shopper name. Required for PIX and boleto.</summary>
    public AdyenShopperName? ShopperName { get; set; }

    /// <summary>Billing address. Required for boleto.</summary>
    public AdyenAddress? BillingAddress { get; set; }

    /// <summary>Two-letter ISO country code driving available payment methods, e.g. <c>BR</c>.</summary>
    public string? CountryCode { get; set; }

    /// <summary>Message shown to the shopper / on the statement.</summary>
    public string? ShopperStatement { get; set; }

    /// <summary>PIX QR-code expiry (ISO 8601 with offset), e.g. <c>2025-10-01T10:00:00-03:00</c>.</summary>
    public string? SessionValidity { get; set; }

    /// <summary>Boleto due date / expiry (ISO 8601).</summary>
    public string? DeliveryDate { get; set; }

    /// <summary>Brazil card installments (parcelado).</summary>
    public AdyenInstallments? Installments { get; set; }

    /// <summary>Free-form key/value data echoed back in webhooks.</summary>
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>Polymorphic payment-method object. Only the fields relevant to the requested method are populated.</summary>
public sealed class AdyenPaymentMethodRequest
{
    /// <summary><c>scheme</c> (card) | <c>pix</c> | <c>boletobancario</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Card number encrypted client-side with Adyen.js/Web Components (or a <c>test_...</c> value in test mode).</summary>
    public string? EncryptedCardNumber { get; set; }

    /// <summary>Encrypted card expiry month.</summary>
    public string? EncryptedExpiryMonth { get; set; }

    /// <summary>Encrypted card expiry year.</summary>
    public string? EncryptedExpiryYear { get; set; }

    /// <summary>Encrypted card security code (CVC/CVV).</summary>
    public string? EncryptedSecurityCode { get; set; }

    /// <summary>Cardholder name.</summary>
    public string? HolderName { get; set; }

    /// <summary>Stored/tokenized payment-method id (used when a card token is supplied).</summary>
    public string? StoredPaymentMethodId { get; set; }

    /// <summary>Card brand, e.g. <c>visa</c>, <c>mc</c>, <c>elo</c>, <c>hipercard</c>.</summary>
    public string? Brand { get; set; }
}

/// <summary>Shopper name (<c>{ firstName, lastName }</c>).</summary>
public sealed class AdyenShopperName
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

/// <summary>Billing / delivery address.</summary>
public sealed class AdyenAddress
{
    public string? Street { get; set; }
    public string? HouseNumberOrName { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    /// <summary>State or province, e.g. <c>SP</c>.</summary>
    public string? StateOrProvince { get; set; }

    /// <summary>Two-letter ISO country code, e.g. <c>BR</c>.</summary>
    public string? Country { get; set; }
}

/// <summary>Brazil card installments (<c>{ value }</c>).</summary>
public sealed class AdyenInstallments
{
    /// <summary>Number of installments (parcelas).</summary>
    public int Value { get; set; }
}
