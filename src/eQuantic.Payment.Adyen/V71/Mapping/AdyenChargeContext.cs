using eQuantic.Payment.Models;

namespace eQuantic.Payment.Adyen.V71.Mapping;

/// <summary>
/// Context for the charge mapper. The Adyen <c>/payments</c> response does not echo the top-level amount
/// (and cannot distinguish credit from debit), so the requested amount and method are carried here.
/// </summary>
public sealed class AdyenChargeContext
{
    /// <summary>The amount from the originating request, used when the response omits it.</summary>
    public Money Amount { get; set; }

    /// <summary>The method from the originating request, used when the response cannot disambiguate it.</summary>
    public PaymentMethodType Method { get; set; }
}
