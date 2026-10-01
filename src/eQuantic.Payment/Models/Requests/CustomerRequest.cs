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

    /// <summary>
    /// The key the gateway recognizes a retry of the customer's creation by, answering it with the customer the
    /// first try created instead of a second one. Derive it from your own id for the customer (and what you send,
    /// since a gateway refuses a key reused with other parameters). Sent when a customer is created; an update
    /// sets the same values again and sends none. Gateways without idempotency keys ignore it.
    /// </summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>
    /// Your own key/value pairs, kept on the gateway's customer (your id for it, for one) where the gateway keeps
    /// metadata. Stripe sends them on create and update, beside the <c>document</c> entry this package writes,
    /// which a <c>document</c> of yours replaces.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

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
