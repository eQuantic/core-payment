using eQuantic.Mapper;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>Maps a Stripe PaymentMethod to the unified <see cref="SavedPaymentMethod"/>.</summary>
public sealed class StripePaymentMethodMapper : IMapper<StripePaymentMethod, SavedPaymentMethod>
{
    public SavedPaymentMethod? Map(StripePaymentMethod? source)
        => source is null
            ? null
            : new SavedPaymentMethod
            {
                Id = source.Id,
                CustomerId = source.Customer,
                Method = source.Type switch
                {
                    "boleto" => PaymentMethodType.Boleto,
                    "pix" => PaymentMethodType.Pix,
                    _ when source.Card?.Funding == "debit" => PaymentMethodType.DebitCard,
                    _ => PaymentMethodType.CreditCard,
                },
                Brand = source.Card?.Brand,
                Last4 = source.Card?.Last4,
                ExpirationMonth = source.Card?.ExpMonth is > 0 and var month ? month : null,
                ExpirationYear = source.Card?.ExpYear is > 0 and var year ? year : null,
                Funding = source.Card?.Funding,
                CreatedAt = source.Created is { } created ? DateTimeOffset.FromUnixTimeSeconds(created) : null,
            };

    public SavedPaymentMethod? Map(StripePaymentMethod? source, SavedPaymentMethod? destination) => Map(source);
}
