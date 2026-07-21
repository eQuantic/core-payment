using eQuantic.Mapper;
using eQuantic.Payment.MercadoPago.Customers.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.MercadoPago.Customers.Mapping;

/// <summary>Maps a Mercado Pago customer to the unified <see cref="Customer"/>.</summary>
public sealed class MercadoPagoCustomerMapper : IMapper<MercadoPagoCustomerResponse, Customer>
{
    public Customer? Map(MercadoPagoCustomerResponse? source)
    {
        if (source is null)
        {
            return null;
        }

        var name = string.Join(' ', new[] { source.FirstName, source.LastName }.Where(p => !string.IsNullOrWhiteSpace(p)));
        return new Customer
        {
            Id = source.Id,
            Name = string.IsNullOrWhiteSpace(name) ? null : name,
            Email = source.Email,
            Document = source.Identification?.Number,
        };
    }

    public Customer? Map(MercadoPagoCustomerResponse? source, Customer? destination) => Map(source);
}
