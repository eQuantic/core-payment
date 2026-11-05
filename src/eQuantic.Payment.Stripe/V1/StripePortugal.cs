using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;

namespace eQuantic.Payment.Stripe.V1;

/// <summary>
/// What Stripe's MB WAY and Multibanco expect: EUR only, from 0.50 EUR to 5,000.00 EUR for MB WAY and to
/// 99,999.00 EUR for Multibanco; the customer's phone number for MB WAY, in the international form
/// <see cref="MbWayDetails.InternationalPhoneNumber"/> gives every gateway, and the customer's email for Multibanco.
/// </summary>
internal static class StripePortugal
{
    public const string Currency = "EUR";

    /// <summary>The least either method charges, in cents: 0.50 EUR.</summary>
    public const long MinimumCents = 50;

    /// <summary>The most <paramref name="method"/> charges, in cents.</summary>
    public static long MaximumCents(PaymentMethodType method) => method switch
    {
        PaymentMethodType.MbWay => 500_000,
        PaymentMethodType.Multibanco => 9_999_900,
        _ => throw new ArgumentOutOfRangeException(nameof(method)),
    };

    /// <summary>What Stripe would refuse anyway of an MB WAY or a Multibanco charge, with a reason the caller can act on.</summary>
    public static PaymentError? Refuse(CreateChargeRequest request)
    {
        var name = Name(request.Method);
        if (!string.Equals(request.Amount.Currency?.Trim(), Currency, StringComparison.OrdinalIgnoreCase))
        {
            return new PaymentError
            {
                Code = "currency_unsupported",
                Message = $"{name} charges in EUR only, and this charge is in '{request.Amount.Currency}': set its amount with Money.Eur(...).",
            };
        }

        var maximum = MaximumCents(request.Method);
        if (request.Amount.AmountInCents is < MinimumCents || request.Amount.AmountInCents > maximum)
        {
            return new PaymentError
            {
                Code = "amount_out_of_range",
                Message = $"{name} charges from EUR {MinimumCents / 100m:0.00} to EUR {maximum / 100m:0.00}, and this charge is {request.Amount}.",
            };
        }

        if (request.Method == PaymentMethodType.MbWay && request.MbWay?.InternationalPhoneNumber is null)
        {
            return new PaymentError
            {
                Code = "mb_way_phone_invalid",
                Message = "MB WAY needs the phone number of the customer's MB WAY account (CreateChargeRequest.MbWay.PhoneNumber), such as +351912345678.",
            };
        }

        if (request.Method == PaymentMethodType.Multibanco && string.IsNullOrWhiteSpace(request.Customer?.Email))
        {
            return new PaymentError
            {
                Code = "multibanco_email_required",
                Message = "Multibanco needs the customer's email (CreateChargeRequest.Customer.Email).",
            };
        }

        return null;
    }

    private static string Name(PaymentMethodType method) => method == PaymentMethodType.MbWay ? "MB WAY" : "Multibanco";
}
