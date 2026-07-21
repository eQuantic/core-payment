namespace eQuantic.Payment.Asaas.V3.Models;

// Faithful wire models for the Asaas v3 customers resource (/customers). Serialized/deserialized as camelCase.

/// <summary>Request body for <c>POST /customers</c>. Required: <c>name</c>, <c>cpfCnpj</c>.</summary>
public sealed class AsaasCustomerRequest
{
    public string Name { get; set; } = string.Empty;

    /// <summary>CPF or CNPJ (digits only).</summary>
    public string CpfCnpj { get; set; } = string.Empty;

    public string? Email { get; set; }

    /// <summary>Landline phone.</summary>
    public string? Phone { get; set; }

    /// <summary>Mobile phone (used by Asaas for SMS/PIX notifications).</summary>
    public string? MobilePhone { get; set; }

    /// <summary>Street.</summary>
    public string? Address { get; set; }

    public string? AddressNumber { get; set; }

    public string? Complement { get; set; }

    /// <summary>Neighborhood (bairro).</summary>
    public string? Province { get; set; }

    /// <summary>Postal code (CEP).</summary>
    public string? PostalCode { get; set; }

    public string? ExternalReference { get; set; }

    public bool? NotificationDisabled { get; set; }
}

/// <summary>The <c>customer</c> object (response of create customer and <c>GET /customers/{id}</c>).</summary>
public sealed class AsaasCustomerResponse
{
    public string Id { get; set; } = string.Empty;
    public string? DateCreated { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? Address { get; set; }
    public string? AddressNumber { get; set; }
    public string? Complement { get; set; }
    public string? Province { get; set; }

    /// <summary>Asaas internal city id (integer), when known.</summary>
    public int? City { get; set; }
    public string? CityName { get; set; }

    /// <summary>State code (UF).</summary>
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? PostalCode { get; set; }
    public string? CpfCnpj { get; set; }

    /// <summary><c>FISICA</c> | <c>JURIDICA</c>.</summary>
    public string? PersonType { get; set; }

    public bool Deleted { get; set; }
    public string? ExternalReference { get; set; }
    public bool NotificationDisabled { get; set; }
}
