using eQuantic.Mapper;
using eQuantic.Payment.Asaas.V3.Models;
using eQuantic.Payment.Models.Requests;

namespace eQuantic.Payment.Asaas.V3.Mapping;

/// <summary>Maps the unified <see cref="CustomerRequest"/> to an Asaas v3 customer request.</summary>
public sealed class AsaasCustomerRequestMapper : IMapper<CustomerRequest, AsaasCustomerRequest>
{
    public AsaasCustomerRequest? Map(CustomerRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var address = source.Address;
        return new AsaasCustomerRequest
        {
            Name = source.Name,
            CpfCnpj = source.DocumentDigits ?? string.Empty,
            Email = source.Email,

            // The unified model carries a single phone; Asaas uses mobilePhone for notifications.
            MobilePhone = source.Phone,
            Address = address?.Line1,
            Complement = address?.Line2,
            PostalCode = address?.ZipCode,
        };
    }

    public AsaasCustomerRequest? Map(CustomerRequest? source, AsaasCustomerRequest? destination) => Map(source);
}
