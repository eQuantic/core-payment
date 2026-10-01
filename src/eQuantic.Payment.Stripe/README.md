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
- A boleto needs the payer's full address (`CustomerRequest.Address`), and it expires in days (`expires_after_days`, 0 to 60).

Source and the other providers: [https://github.com/eQuantic/core-payment](https://github.com/eQuantic/core-payment).
