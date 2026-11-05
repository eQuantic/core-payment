namespace eQuantic.Payment.Models;

/// <summary>
/// Payment methods: the four of the Brazilian market, and MB WAY and Multibanco, Portugal's. New members go last, so
/// the numeric value of every member stays what it was.
/// </summary>
public enum PaymentMethodType
{
    CreditCard,
    DebitCard,
    Pix,
    Boleto,

    /// <summary>
    /// MB WAY, Portugal's wallet: the customer approves the payment in the MB WAY app on their phone. In EUR only,
    /// with the phone number in <see cref="Requests.CreateChargeRequest.MbWay"/>.
    /// </summary>
    MbWay,

    /// <summary>
    /// Multibanco, Portugal's voucher: the customer pays an entity and a reference at an ATM or in online banking.
    /// In EUR only; the voucher comes back in <see cref="Results.Charge.Multibanco"/>.
    /// </summary>
    Multibanco,
}
