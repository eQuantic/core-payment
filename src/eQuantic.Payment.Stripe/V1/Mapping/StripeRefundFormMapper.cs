using eQuantic.Mapper;
using eQuantic.Payment.Models.Requests;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>Maps a unified <see cref="RefundRequest"/> to the Stripe <c>POST /v1/refunds</c> form body.</summary>
public sealed class StripeRefundFormMapper : IMapper<RefundRequest, StripeForm>
{
    public StripeForm? Map(RefundRequest? source)
    {
        if (source is null)
        {
            return null;
        }

        var form = new StripeForm { { "payment_intent", source.ChargeId } };
        if (source.Amount is { } amount)
        {
            form.Add("amount", amount.AmountInCents.ToString());
        }

        return form;
    }

    public StripeForm? Map(RefundRequest? source, StripeForm? destination) => Map(source);
}
