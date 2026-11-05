using eQuantic.Mapper;
using eQuantic.Payment.Models.Results;
using eQuantic.Payment.Stripe.V1.Models;

namespace eQuantic.Payment.Stripe.V1.Mapping;

/// <summary>
/// Maps the PaymentIntent a create returns to the unified <see cref="Charge"/>, as <see cref="StripeChargeMapper"/>
/// does, adding its client secret when <see cref="StripeChargeContext.ConfirmsInBrowser"/>: the customer's browser
/// confirms that charge with it, and no other charge needs it. A mapper with a context resolves by its context
/// interface only, so this one stands beside <see cref="StripeChargeMapper"/> rather than replacing it.
/// </summary>
public sealed class StripeCreatedChargeMapper : IMapper<StripePaymentIntent, Charge, StripeChargeContext>
{
    public StripeChargeContext? Context { get; set; }

    public Charge? Map(StripePaymentIntent? source) =>
        StripeChargeMapper.Map(source, Context?.ConfirmsInBrowser == true ? source?.ClientSecret : null);

    public Charge? Map(StripePaymentIntent? source, Charge? destination) => Map(source);
}
