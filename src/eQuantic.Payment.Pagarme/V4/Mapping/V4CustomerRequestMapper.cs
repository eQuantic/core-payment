using eQuantic.Mapper;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Pagarme.V4.Models;

namespace eQuantic.Payment.Pagarme.V4.Mapping;

/// <summary>Maps the unified <see cref="CustomerRequest"/> to the legacy Pagar.me v4 customer request.</summary>
public sealed class V4CustomerRequestMapper : IMapper<CustomerRequest, V4CustomerRequest>
{
    public V4CustomerRequest? Map(CustomerRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        return new V4CustomerRequest
        {
            Name = source.Name,
            Email = source.Email,
            ExternalId = source.Email,
            Type = source.DocumentType == eQuantic.Payment.Models.Requests.DocumentType.Cnpj ? "corporation" : "individual",
            Documents = source.DocumentDigits is { } doc
                ?
                [
                    new V4Document { Type = source.DocumentType == eQuantic.Payment.Models.Requests.DocumentType.Cnpj ? "cnpj" : "cpf", Number = doc },
                ]
                : null,
            PhoneNumbers = source.Phone is null ? null : [source.Phone],
        };
    }

    public V4CustomerRequest? Map(CustomerRequest? source, V4CustomerRequest? destination) => Map(source);
}
