using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Results;

namespace eQuantic.Payment.Abstractions;

/// <summary>
/// The answer a provider gives to a charge in a payment method it does not take, before anything is sent: a failed
/// <see cref="PaymentResponse{T}"/> with <see cref="Code"/>, never an exception.
/// </summary>
public static class UnsupportedPaymentMethodType
{
    public const string Code = "payment_method_type_unsupported";

    /// <summary>The refusal of <paramref name="method"/> by <paramref name="provider"/>.</summary>
    public static PaymentResponse<Charge> Refuse(ProviderInfo provider, PaymentMethodType method)
        => PaymentResponse<Charge>.Fail(provider, new PaymentError
        {
            Code = Code,
            Message = $"Provider '{provider.Key}' does not take {method} payments.",
        });

    /// <summary>
    /// Whether <paramref name="method"/> is one of the four Brazilian methods (card, debit card, Pix, boleto), the ones
    /// every provider modeled before MB WAY and Multibanco took. A provider still checks which of them its API covers.
    /// </summary>
    public static bool IsBrazilian(PaymentMethodType method)
        => method is PaymentMethodType.CreditCard or PaymentMethodType.DebitCard or PaymentMethodType.Pix or PaymentMethodType.Boleto;
}
