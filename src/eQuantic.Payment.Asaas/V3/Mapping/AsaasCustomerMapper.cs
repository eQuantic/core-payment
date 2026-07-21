using eQuantic.Mapper;
using eQuantic.Payment.Asaas.V3.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Asaas.V3.Mapping;

/// <summary>Maps an Asaas v3 customer to the unified <see cref="Customer"/>.</summary>
public sealed class AsaasCustomerMapper : IMapper<AsaasCustomerResponse, Customer>
{
    public Customer? Map(AsaasCustomerResponse? source)
        => source is null
            ? null
            : new Customer
            {
                Id = source.Id,
                Name = source.Name,
                Email = source.Email,
                Document = source.CpfCnpj,
            };

    public Customer? Map(AsaasCustomerResponse? source, Customer? destination) => Map(source);
}
