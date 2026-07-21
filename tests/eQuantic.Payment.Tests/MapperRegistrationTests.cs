using eQuantic.Mapper;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Pagarme.V4.Models;
using eQuantic.Payment.Pagarme.V5.Models;
using eQuantic.Payment.Stripe.V1.Mapping;
using eQuantic.Payment.Stripe.V1.Models;
using eQuantic.Payment.Tests.Fakes;

namespace eQuantic.Payment.Tests;

/// <summary>Verifies that every provider mapping is registered as an eQuantic.Mapper mapper and resolvable via the factory.</summary>
public class MapperRegistrationTests
{
    private readonly IMapperFactory _mappers = TestMapperFactory.Create();

    [Fact]
    public void Pagarme_v5_mappers_are_registered()
    {
        Assert.True(_mappers.TryGetMapper<CreateChargeRequest, V5OrderRequest>(out _));
        Assert.True(_mappers.TryGetMapper<V5ChargeResponse, Charge>(out _));
        Assert.True(_mappers.TryGetMapper<V5OrderResponse, Charge>(out _));
        Assert.True(_mappers.TryGetMapper<V5CustomerResponse, Customer>(out _));
        Assert.True(_mappers.TryGetMapper<V5ChargeResponse, Refund>(out _));
    }

    [Fact]
    public void Pagarme_v4_mappers_are_registered()
    {
        Assert.True(_mappers.TryGetMapper<CreateChargeRequest, V4TransactionRequest>(out _));
        Assert.True(_mappers.TryGetMapper<V4TransactionResponse, Charge>(out _));
        Assert.True(_mappers.TryGetMapper<V4CustomerResponse, Customer>(out _));
    }

    [Fact]
    public void Stripe_mappers_are_registered()
    {
        // Response → unified
        Assert.True(_mappers.TryGetMapper<StripePaymentIntent, Charge>(out _));
        Assert.True(_mappers.TryGetMapper<StripeCustomer, Customer>(out _));
        Assert.True(_mappers.TryGetMapper<StripeRefund, Refund>(out _));

        // Request → form (form-building runs through eQuantic.Mapper too)
        Assert.True(_mappers.TryGetMapper<CreateChargeRequest, StripeForm, StripeRequestContext>(new StripeRequestContext(), out _));
        Assert.True(_mappers.TryGetMapper<RefundRequest, StripeForm>(out _));
        Assert.True(_mappers.TryGetMapper<CustomerRequest, StripeForm>(out _));
    }
}
