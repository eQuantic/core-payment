namespace eQuantic.Payment.Models.Requests;

/// <summary>Provider-agnostic customer data.</summary>
public sealed class CustomerRequest
{
    public required string Name { get; init; }
    public required string Email { get; init; }

    /// <summary>CPF or CNPJ (digits only preferred).</summary>
    public string? Document { get; init; }

    public string? Phone { get; init; }

    /// <summary>Billing address. Required by some providers for boleto.</summary>
    public AddressRequest? Address { get; init; }

    /// <summary>Digits-only view of <see cref="Document"/>.</summary>
    public string? DocumentDigits => Document is null
        ? null
        : new string(Document.Where(char.IsDigit).ToArray());

    /// <summary>Infers the document type from its length: 11 digits = CPF, 14 = CNPJ.</summary>
    public DocumentType? DocumentType => DocumentDigits?.Length switch
    {
        11 => Requests.DocumentType.Cpf,
        14 => Requests.DocumentType.Cnpj,
        _ => null,
    };
}

public enum DocumentType
{
    Cpf,
    Cnpj,
}
