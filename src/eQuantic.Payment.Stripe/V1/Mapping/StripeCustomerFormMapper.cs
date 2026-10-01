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

    /// <summary>
    /// What an update sends besides <see cref="Map(CustomerRequest?)"/>'s fields. Stripe keeps a field an update leaves
    /// out, so the optional ones <paramref name="request"/> does not carry (phone, address, the address's second line,
    /// the document) go empty, which clears them, and the customer ends up as the request says. Metadata entries of
    /// yours that the request no longer carries stay.
    /// </summary>
    internal static void ClearOmitted(StripeForm form, CustomerRequest request)
    {
        if (request.Phone is null)
        {
            form.Add("phone", string.Empty);
        }

        if (request.Address is null)
        {
            form.Add("address", string.Empty);
        }
        else if (request.Address.Line2 is null)
        {
            form.Add("address[line2]", string.Empty);
        }

        if (request.DocumentDigits is null && request.Metadata?.ContainsKey("document") != true)
        {
            form.Add("metadata[document]", string.Empty);
        }
    }
}
