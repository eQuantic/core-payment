# eQuantic.Payment.Stripe

Stripe for [eQuantic.Payment](https://github.com/eQuantic/core-payment). A typed client for Stripe's REST API `v1`, pinned to a dated `Stripe-Version`, and the adapter that speaks eQuantic.Payment's unified model: cards, Pix, boleto, MB WAY and Multibanco through PaymentIntents, refunds and customers.

## Install

```bash
dotnet add package eQuantic.Payment.Stripe
```

## Register

```csharp
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Stripe;

services.AddPayments(payments => payments
    .AddStripe(o =>
    {
        o.ApiKey        = configuration["Stripe:ApiKey"]!;
        o.Version       = StripeApiVersion.V2025_04_30_Basil;
        o.WebhookSecret = configuration["Stripe:WebhookSecret"]; // whsec_…, to verify notifications
    }, asDefault: true));
```

The provider then answers through `IPaymentProvider`, from `IPaymentProviderFactory.Get("stripe")` or as
the default, and every response names the version that answered.

## Versions

| Option | What it talks to |
|---|---|
| `StripeApiVersion.V2024_06_20` | the `Stripe-Version` header `2024-06-20` |
| `StripeApiVersion.V2025_04_30_Basil` (default) | the `Stripe-Version` header `2025-04-30.basil` |
| `StripeApiVersion.V2026_09_30_Endive` | the `Stripe-Version` header `2026-09-30.endive` |

At Endive, a PaymentIntent or a SetupIntent no longer takes `payment_method_types` (Stripe answers 400,
`payment_method_types_no_longer_supported`), so the provider names the one type each request is for in
`allowed_payment_method_types` there, and in `payment_method_types` at the versions before it, which do not
know the new field: `card`, `pix`, `boleto`, `mb_way` or `multibanco`. Nothing else in a request changes with the version. The events Stripe sends follow the
version of the webhook endpoint, set in Stripe's Dashboard, not this one: move both together.

## Saved cards, charged with the customer away

A subscription saves a card once and charges it on every cycle without the customer. The card is saved
through a SetupIntent that the customer's browser confirms with Stripe.js, so no card number reaches you:

```csharp
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;

var customer = await stripe.Customers.CreateAsync(new CustomerRequest
{
    Name = name, Email = email, Document = cpfOrCnpj, Address = address,
    IdempotencyKey = $"{accountId}:customer",
    Metadata = new Dictionary<string, string> { ["account_id"] = accountId },
});
var setup = await stripe.PaymentMethods.SetupAsync(new PaymentMethodSetupRequest
{
    CustomerId = customer.Data!.Id,
    IdempotencyKey = $"{accountId}:setup",
});
// The browser confirms setup.Data!.ClientSecret with stripe.confirmCardSetup(...); then:
var saved = await stripe.PaymentMethods.GetSetupAsync(setup.Data.Id); // Succeeded, with PaymentMethodId

var renewal = await stripe.Charges.CreateAsync(new CreateChargeRequest
{
    Amount = Money.Brl(49.90m),
    Method = PaymentMethodType.CreditCard,
    CustomerId = customer.Data.Id,
    Card = new CardDetails { PaymentMethodId = saved.Data!.PaymentMethodId },
    OffSession = true,
    IdempotencyKey = $"{attemptId}:create",
    AttemptedAt = attemptedAt,
});
```

A card Stripe declines, or one that asks for authentication with the customer away, is an outcome, not an
exception: `Success` is false, `Error.Code` is the decline code (`insufficient_funds`, `generic_decline`,
`authentication_required`…) and `Data` is still the charge, its id and its `Failed` status. After
`authentication_required`, the customer has to come back to pay. `PaymentMethods.ListAsync` lists every
one of a customer's saved cards (brand, last four digits, expiry), page after page, and `DetachAsync`
removes one. A setup's client secret comes back in `ClientSecret` only: the `RawResponse` kept for
auditing has it redacted.

`Customers.UpdateAsync` keeps the customer in step with yours: what the request leaves out (a phone, an
address or its second line, a document) is cleared at Stripe too. A customer is created under its
`IdempotencyKey`, so a retried create answers with the customer the first try made (a key reused with
other parameters is refused), and keeps its `Metadata` (your id for it, for one) beside the `document`
entry this package writes.

A Brazilian Stripe account takes Visa and Mastercard credit cards and foreign debit cards: no Elo,
Hipercard, Amex, Brazilian debit or installments.

## Boleto

A boleto needs its payer whole, in `CreateChargeRequest.Customer`: name, email, CPF or CNPJ and full address.
Without any of them, the request is refused before it reaches Stripe (`boleto_payer_incomplete`). The name and
address go in ASCII (`São João` becomes `Sao Joao`), as Stripe asks. `Boleto.DueDate` becomes
`expires_after_days`, counted in São Paulo days, where Stripe ends the voucher at 23:59; 0 to 60 days.

The charge comes back pending with the voucher: `Boleto.DigitableLine`, `PdfUrl`, `HostedUrl` and `ExpiresAt`.
Stripe confirms a payment up to one business day after it is made (`payment_intent.succeeded`), and a voucher
that expires unpaid fails the PaymentIntent (`payment_intent.payment_failed`), which reads as `Expired`. A
boleto cannot be refunded or disputed through Stripe.

## MB WAY and Multibanco

Portugal's two methods, for an account that takes them: registered in Europe, or in the US. Stripe refuses
both for an account in Brazil. Both are in EUR only, from 0.50 EUR to 5,000.00 EUR for MB WAY and to
99,999.00 EUR for Multibanco; a charge in another currency (`currency_unsupported`, as a `Money` left at
its BRL default would be) or outside those limits (`amount_out_of_range`) is refused before it reaches
Stripe. Neither takes manual capture or a charge with the customer away.

```csharp
var mbWay = await stripe.Charges.CreateAsync(new CreateChargeRequest
{
    Amount      = Money.Eur(25.00m),
    Method      = PaymentMethodType.MbWay,
    MbWay       = new MbWayDetails { PhoneNumber = "912 345 678" },
    ReferenceId = "order-42",
});

var multibanco = await stripe.Charges.CreateAsync(new CreateChargeRequest
{
    Amount      = Money.Eur(99.00m),
    Method      = PaymentMethodType.Multibanco,
    Customer    = new CustomerRequest { Name = "Joana Silva", Email = "joana@example.com" },
    ReferenceId = "order-43",
});
var voucher = multibanco.Data!.Multibanco!; // Entity, Reference, ExpiresAt, HostedUrl
```

MB WAY sends the phone as `billing_details[phone]`, in international form (`+351912345678` from
`912 345 678`, `00351 912 345 678` or `+351 912-345-678`); without a phone number, or with something that is
not one, the charge is refused with `mb_way_phone_invalid`. Stripe pushes the payment to the customer's MB
WAY app, and the charge stays `Pending` while they approve it, for up to four minutes: approved, it is
`Paid`; declined, `Failed`; left unanswered, `Expired`.

Multibanco sends the customer's email as `billing_details[email]`; without one, the charge is refused with
`multibanco_email_required`. The charge comes back `Pending` with the voucher, which the customer pays at an
ATM or in online banking within seven days, for the charge's amount. Read again once paid, it keeps the
entity and reference. A voucher past its expiry reads as `Processing` for up to four days, while Stripe waits
for a late bank notice, and then as `Expired`. Canceling the charge invalidates its voucher.

Neither settles while the customer is on the page, so the order moves on the events Stripe sends:
`payment_intent.succeeded`, `payment_intent.payment_failed`, `payment_intent.canceled` and
`payment_intent.processing`, each carrying the charge (below).

## Notifications

With the endpoint's signing secret in `WebhookSecret`, the provider verifies what Stripe sends and parses it:

```csharp
using eQuantic.Payment.Models.Requests;

var response = stripe.Notifications.Verify(new NotificationRequest
{
    Body    = rawBody,                  // the request body exactly as it arrived
    Headers = headers,                  // Stripe-Signature among them
});

if (response.Success)
{
    var notification = response.Data!; // Id, Type, CreatedAt, ObjectId, ObjectType, LiveMode
    var charge = notification.Charge;  // the PaymentIntent, when the event is about one: Status, Method, ReferenceId…
}
```

A forged or altered event, one signed more than `WebhookTolerance` (five minutes) away from now, one
without a valid `Stripe-Signature`, and a signed body that is not a Stripe event all answer a failure with
its reason. Stripe retries an event for up to
three days and promises no order, so deduplicate by `Id` and read the object again before acting on it.

An event about a PaymentIntent carries it as the unified charge (`Charge`), as the event rendered it, in the
version of the webhook endpoint: its status, method, amount, reference id and, for Multibanco, its voucher.
An event about anything else carries none, and so does a PaymentIntent the model cannot read: the event is
still verified, to be acknowledged.

## Good to know

- Requests are form-encoded with a Bearer secret key; every PaymentIntent is read with `latest_charge` expanded, so card, Pix, boleto and Multibanco details come back inline.
- A PaymentIntent waiting for a payment method after a failed attempt reads as `Failed`, or `Expired` when the attempt expired (an unpaid boleto, Pix or Multibanco voucher, an unanswered MB WAY push); one that has not been attempted, or waits for the customer (`requires_action`), reads as `Pending`, and one Stripe is still settling (`processing`) as `Processing`.
- An `IdempotencyKey` on a create, capture, cancel or refund goes as `Idempotency-Key`: for 24 hours, Stripe answers a retry under the same key with the first response, and refuses one whose parameters differ.

Source and the other providers: [https://github.com/eQuantic/core-payment](https://github.com/eQuantic/core-payment).
