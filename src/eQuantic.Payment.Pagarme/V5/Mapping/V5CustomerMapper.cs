using eQuantic.Mapper;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Pagarme.V5.Models;

namespace eQuantic.Payment.Pagarme.V5.Mapping;

/// <summary>Maps a Pagar.me v5 customer to the unified <see cref="Customer"/>.</summary>
public sealed class V5CustomerMapper : IMapper<V5CustomerResponse, Customer>
{
    public Customer? Map(V5CustomerResponse? source)
        => source is null
            ? null
            : new Customer
            {
                Id = source.Id,
                Name = source.Name,
                Email = source.Email,
                Document = source.Document,
            };

    public Customer? Map(V5CustomerResponse? source, Customer? destination) => Map(source);
}
