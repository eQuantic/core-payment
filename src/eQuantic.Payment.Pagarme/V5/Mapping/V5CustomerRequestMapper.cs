using eQuantic.Mapper;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Pagarme.V5.Models;

namespace eQuantic.Payment.Pagarme.V5.Mapping;

/// <summary>Maps the unified <see cref="CustomerRequest"/> to the Pagar.me v5 customer request.</summary>
public sealed class V5CustomerRequestMapper : IMapper<CustomerRequest, V5CustomerRequest>
{
    public V5CustomerRequest? Map(CustomerRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        return new V5CustomerRequest
        {
            Name = source.Name,
            Email = source.Email,
            Document = source.DocumentDigits,
            DocumentType = source.DocumentType switch
            {
                eQuantic.Payment.Models.Requests.DocumentType.Cpf => "CPF",
                eQuantic.Payment.Models.Requests.DocumentType.Cnpj => "CNPJ",
                _ => null,
            },
            Type = source.DocumentType == eQuantic.Payment.Models.Requests.DocumentType.Cnpj ? "company" : "individual",
            Address = MapAddress(source.Address),
            Phones = MapPhones(source.Phone),
        };
    }

    public V5CustomerRequest? Map(CustomerRequest? source, V5CustomerRequest? destination) => Map(source);

    private static V5AddressRequest? MapAddress(AddressRequest? address)
        => address is null
            ? null
            : new V5AddressRequest
            {
                Line1 = address.Line1,
                Line2 = address.Line2,
                ZipCode = address.ZipCode,
                City = address.City,
                State = address.State,
                Country = address.Country,
            };

    private static V5PhonesRequest? MapPhones(string? phone)
    {
        // The unified phone is a single free-form string; only forwarded when it can be split into DDD + number.
        var digits = phone is null ? null : new string(phone.Where(char.IsDigit).ToArray());
        if (digits is null || digits.Length < 10)
        {
            return null;
        }

        if (digits.Length > 11 && digits.StartsWith("55"))
        {
            digits = digits[2..];
        }

        return new V5PhonesRequest
        {
            MobilePhone = new V5PhoneRequest { CountryCode = "55", AreaCode = digits[..2], Number = digits[2..] },
        };
    }
}
