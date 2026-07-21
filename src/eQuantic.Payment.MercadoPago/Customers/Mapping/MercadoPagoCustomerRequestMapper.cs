using eQuantic.Mapper;
using eQuantic.Payment.MercadoPago.Customers.Models;
using eQuantic.Payment.Models.Requests;

namespace eQuantic.Payment.MercadoPago.Customers.Mapping;

/// <summary>Maps the unified <see cref="CustomerRequest"/> to the Mercado Pago customer request.</summary>
public sealed class MercadoPagoCustomerRequestMapper : IMapper<CustomerRequest, MercadoPagoCustomerRequest>
{
    public MercadoPagoCustomerRequest? Map(CustomerRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var (first, last) = MercadoPagoConventions.SplitName(source.Name);
        return new MercadoPagoCustomerRequest
        {
            Email = source.Email,
            FirstName = first,
            LastName = last,
            Identification = MercadoPagoConventions.ToIdentification(source),
            Phone = MercadoPagoConventions.ToPhone(source.Phone),
        };
    }

    public MercadoPagoCustomerRequest? Map(CustomerRequest? source, MercadoPagoCustomerRequest? destination) => Map(source);
}
