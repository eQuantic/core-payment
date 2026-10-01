using eQuantic.Mapper;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>Maps a Stripe SetupIntent to the unified <see cref="PaymentMethodSetup"/>.</summary>
public sealed class StripeSetupIntentMapper : IMapper<StripeSetupIntent, PaymentMethodSetup>
{
    public PaymentMethodSetup? Map(StripeSetupIntent? source)
        => source is null
            ? null
            : new PaymentMethodSetup
            {
                Id = source.Id,
                Status = StripeStatusMapper.FromSetupIntent(source.Status),
                ProviderStatus = source.Status,
                ClientSecret = source.ClientSecret,
                CustomerId = source.Customer,
                PaymentMethodId = source.PaymentMethod,
                LastError = source.LastSetupError is { } error ? StripeErrorMapper.ToError(error, statusCode: null) : null,
                CreatedAt = source.Created is { } created ? DateTimeOffset.FromUnixTimeSeconds(created) : null,
            };

    public PaymentMethodSetup? Map(StripeSetupIntent? source, PaymentMethodSetup? destination) => Map(source);
}
