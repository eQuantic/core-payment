using eQuantic.Payment.MercadoPago.Payments.Models;

namespace eQuantic.Payment.MercadoPago.Customers.Models;

// Faithful wire models for the shared Mercado Pago customers resource (/v1/customers), used by both API versions.

/// <summary>Request body for <c>POST /v1/customers</c>.</summary>
public sealed class MercadoPagoCustomerRequest
{
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public MercadoPagoPhone? Phone { get; set; }
    public MercadoPagoIdentification? Identification { get; set; }
    public string? Description { get; set; }
}

/// <summary>The <c>customer</c> object (response of create/get customer).</summary>
public sealed class MercadoPagoCustomerResponse
{
    /// <summary>Customer id (string).</summary>
    public string Id { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public MercadoPagoPhone? Phone { get; set; }
    public MercadoPagoIdentification? Identification { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset? DateCreated { get; set; }
    public DateTimeOffset? DateLastUpdated { get; set; }
    public bool? LiveMode { get; set; }
}
