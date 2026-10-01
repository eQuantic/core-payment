using eQuantic.Mapper;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>
/// Maps a unified <see cref="CustomerRequest"/> to the Stripe <c>POST /v1/customers</c> form body, which an update
/// (<c>POST /v1/customers/{id}</c>) takes too.
/// </summary>
public sealed class StripeCustomerFormMapper : IMapper<CustomerRequest, StripeForm>
{
    public StripeForm? Map(CustomerRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var form = new StripeForm
        {
            { "name", source.Name },
            { "email", source.Email },
        };

        if (source.Phone is { } phone)
        {
            form.Add("phone", phone);
        }

        if (source.Metadata is not null)
        {
            foreach (var (key, value) in source.Metadata)
            {
                form.Add($"metadata[{key}]", value);
            }
        }

        if (source.DocumentDigits is { } doc && source.Metadata?.ContainsKey("document") != true)
        {
            form.Add("metadata[document]", doc);
        }

        StripePaymentIntentFormMapper.AppendAddress(form, "address", source.Address);
        return form;
    }

    public StripeForm? Map(CustomerRequest? source, StripeForm? destination) => Map(source);
}
