using eQuantic.Mapper;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>Maps a Stripe customer to the unified <see cref="Customer"/>.</summary>
public sealed class StripeCustomerMapper : IMapper<StripeCustomer, Customer>
{
    public Customer? Map(StripeCustomer? source)
        => source is null
            ? null
            : new Customer
            {
                Id = source.Id,
                Name = source.Name,
                Email = source.Email,
                Document = source.Metadata?.GetValueOrDefault("document"),
            };

    public Customer? Map(StripeCustomer? source, Customer? destination) => Map(source);
}
