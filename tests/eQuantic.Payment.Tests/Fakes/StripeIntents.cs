using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;

namespace eQuantic.Payment.Tests.Fakes;

/// <summary>
/// One of each intent the Stripe provider creates, for the tests that compare what it sends at each API version, to
/// stubbed HTTP, to stripe-mock and to Stripe's test mode alike.
/// </summary>
public static class StripeIntents
{
    /// <summary>The kinds <see cref="Charge"/> builds, by name, for theories.</summary>
    public const string Card = "card", SavedCard = "saved card", BrowserCard = "browser card", Pix = "pix", Boleto = "boleto", MbWay = "mb way", Multibanco = "multibanco";

    public static CreateChargeRequest Charge(string kind, DateOnly today, string customerId = "cus_123", string paymentMethodId = "pm_card_visa") => kind switch
    {
        Card => new CreateChargeRequest
        {
            Amount = Money.Brl(49.90m),
            Method = PaymentMethodType.CreditCard,
            Card = new CardDetails { Token = "tok_visa" },
        },
        BrowserCard => BrowserCardCharge(),

        SavedCard => new CreateChargeRequest
        {
            Amount = Money.Brl(49.90m),
            Method = PaymentMethodType.CreditCard,
            CustomerId = customerId,
            Card = new CardDetails { PaymentMethodId = paymentMethodId },
            OffSession = true,
        },
        Pix => new CreateChargeRequest
        {
            Amount = Money.Brl(25.00m),
            Method = PaymentMethodType.Pix,
        },
        Boleto => new CreateChargeRequest
        {
            Amount = Money.Brl(99.00m),
            Method = PaymentMethodType.Boleto,
            Customer = Payer(),
            Boleto = new BoletoDetails { DueDate = today.AddDays(3) },
        },
        MbWay => new CreateChargeRequest
        {
            Amount = Money.Eur(25.00m),
            Method = PaymentMethodType.MbWay,
            MbWay = new MbWayDetails { PhoneNumber = "912 345 678" },
        },
        Multibanco => new CreateChargeRequest
        {
            Amount = Money.Eur(99.00m),
            Method = PaymentMethodType.Multibanco,
            Customer = Portuguese(),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    /// <summary>
    /// A card charge with no card, the customer present: the customer's browser confirms it with what the Payment
    /// Element collects.
    /// </summary>
    public static CreateChargeRequest BrowserCardCharge(string reference = "order-44", string email = "joana.silva@example.com") => new()
    {
        Amount = Money.Eur(49.90m),
        Method = PaymentMethodType.CreditCard,
        Customer = Portuguese(email),
        ReferenceId = reference,
    };

    /// <summary>A customer in Portugal: name and e-mail, which is all Multibanco needs.</summary>
    public static CustomerRequest Portuguese(string email = "joana.silva@example.com") => new()
    {
        Name = "Joana Silva",
        Email = email,
    };

    public static PaymentMethodSetupRequest CardSetup(string customerId = "cus_123") => new() { CustomerId = customerId };

    /// <summary>A boleto's payer: name, e-mail, a CPF that passes its check digits, and a full address.</summary>
    public static CustomerRequest Payer() => new()
    {
        Name = "Maria Silva",
        Email = "maria.silva@example.com",
        Document = "529.982.247-25",
        Address = new AddressRequest { Line1 = "Av. Paulista, 1000", City = "São Paulo", State = "SP", ZipCode = "01310-100" },
    };
}
