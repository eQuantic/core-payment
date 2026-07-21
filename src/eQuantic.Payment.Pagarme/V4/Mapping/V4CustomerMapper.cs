using eQuantic.Mapper;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Pagarme.V4.Models;

namespace eQuantic.Payment.Pagarme.V4.Mapping;

/// <summary>Maps a legacy Pagar.me v4 customer to the unified <see cref="Customer"/>.</summary>
public sealed class V4CustomerMapper : IMapper<V4CustomerResponse, Customer>
{
    public Customer? Map(V4CustomerResponse? source)
        => source is null
            ? null
            : new Customer
            {
                Id = source.Id.ToString(),
                Name = source.Name,
                Email = source.Email,
                Document = source.Documents?.FirstOrDefault()?.Number ?? source.DocumentNumber,
            };

    public Customer? Map(V4CustomerResponse? source, Customer? destination) => Map(source);
}
