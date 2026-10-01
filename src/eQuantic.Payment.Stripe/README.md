# eQuantic.Payment.Stripe

Stripe for [eQuantic.Payment](https://github.com/eQuantic/core-payment). A typed client for Stripe's REST API `v1`, pinned to a dated `Stripe-Version`, and the adapter that speaks eQuantic.Payment's unified model: cards, Pix and boleto through PaymentIntents, refunds and customers.

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

## Saved cards, charged with the customer away

A subscription saves a card once and charges it on every cycle without the customer. The card is saved
through a SetupIntent that the customer's browser confirms with Stripe.js, so no card number reaches you:

```csharp
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;

var customer = await stripe.Customers.CreateAsync(payer);            // CustomerRequest: name, email, CPF/CNPJ, address
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
`authentication_required`, the customer has to come back to pay. `PaymentMethods.ListAsync` lists a
customer's saved cards (brand, last four digits, expiry) and `DetachAsync` removes one;
`Customers.UpdateAsync` keeps the customer in step with yours.

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
}
```

A forged or altered event, one signed more than `WebhookTolerance` (five minutes) away from now, one
without a valid `Stripe-Signature`, and a signed body that is not a Stripe event all answer a failure with
its reason. Stripe retries an event for up to
three days and promises no order, so deduplicate by `Id` and read the object again before acting on it.

## Good to know

- Requests are form-encoded with a Bearer secret key; every PaymentIntent is read with `latest_charge` expanded, so card, Pix and boleto details come back inline.
- A PaymentIntent waiting for a payment method after a failed attempt reads as `Failed`, or `Expired` when the attempt expired (an unpaid boleto or Pix); one that has not been attempted reads as `Pending`.
- An `IdempotencyKey` on a create, capture, cancel or refund goes as `Idempotency-Key`: for 24 hours, Stripe answers a retry under the same key with the first response, and refuses one whose parameters differ.

Source and the other providers: [https://github.com/eQuantic/core-payment](https://github.com/eQuantic/core-payment).
