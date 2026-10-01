using eQuantic.Mapper;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>
/// Maps a unified <see cref="PaymentMethodSetupRequest"/> to the Stripe <c>POST /v1/setup_intents</c> form body:
/// a card, saved for the customer to be charged with the customer away (<c>usage=off_session</c>).
/// </summary>
public sealed class StripeSetupIntentFormMapper : IMapper<PaymentMethodSetupRequest, StripeForm>
{
    public StripeForm? Map(PaymentMethodSetupRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var form = new StripeForm
        {
            { "customer", source.CustomerId },
            { "payment_method_types[]", "card" },
            { "usage", "off_session" },
        };

        if (source.Metadata is not null)
        {
            foreach (var (key, value) in source.Metadata)
            {
                form.Add($"metadata[{key}]", value);
            }
        }

        return form;
    }

    public StripeForm? Map(PaymentMethodSetupRequest? source, StripeForm? destination) => Map(source);
}
