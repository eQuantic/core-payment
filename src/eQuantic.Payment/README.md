# eQuantic.Payment

One contract for every payment gateway: the same request, the same response, and the provider and API
version that answered, always. The gateways live in their own packages and plug into this one.

## Install

```bash
dotnet add package eQuantic.Payment
dotnet add package eQuantic.Payment.Stripe   # or any other provider package
```

## Register

```csharp
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Stripe;

services.AddPayments(payments => payments
    .AddStripe(o => o.ApiKey = configuration["Stripe:ApiKey"]!, asDefault: true));
```

## Charge

```csharp
using eQuantic.Payment.Abstractions;
using eQuantic.Payment.Models;
using eQuantic.Payment.Models.Requests;

var provider = factory.GetDefault();                 // IPaymentProviderFactory, from DI
var response = await provider.Charges.CreateAsync(new CreateChargeRequest
{
    Amount = Money.Brl(49.90m),
    Method = PaymentMethodType.Pix,
});

if (response.Success)
{
    Console.WriteLine($"{response.Data!.Id} via {response.Provider.Key}");
}
```

## What is in it

- **Contracts**: `IPaymentProvider` with its `Charges`, `Refunds`, `Customers`, `PaymentMethods` and
  `Notifications` operations, and `IPaymentProviderFactory`, which resolves a provider by name, by name and
  version, or the default. `Notifications.Verify` checks that a webhook came from the gateway before parsing
  it; `PaymentMethods` saves a customer's card to charge it later with the customer away
  (`CreateChargeRequest.OffSession`); `Customers.UpdateAsync` keeps a customer in step. A provider that does
  not do one of these yet answers with a failure.
- **The unified model**: `CreateChargeRequest`, `CaptureRequest`, `CancelRequest`, `RefundRequest`,
  `CustomerRequest`; `Charge`, `Customer`, `Refund`, with the Pix, boleto, Multibanco and card details each
  method returns, and the client secret of a card charge the customer's browser confirms (`Charge.ClientSecret`); `Money` in the currency's smallest unit, BRL unless told otherwise (`Money.Brl`,
  `Money.Eur`). The methods are card, debit card, Pix and boleto for Brazil, and MB WAY and Multibanco for
  Portugal, in euros; a provider that does not take a method answers `payment_method_type_unsupported`
  (`UnsupportedPaymentMethodType`) before sending anything.
- **Notifications**: a verified `PaymentNotification` names the event and its object, and carries the charge
  when the object is one (`PaymentNotification.Charge`), so a webhook knows the payment's status, method and
  reference id without another call.
- **`PaymentResponse<T>`**: the result or a `PaymentError`, the provider and version that answered, and
  the gateway's raw body for auditing. A failure can still carry what the gateway returned about the object:
  a declined charge comes back with its id and status.
- **The registration**: `AddPayments(...)` and the `PaymentBuilder` the provider packages extend.

## MB WAY and Multibanco

Portugal's two methods go through the same `Charges.CreateAsync` as any other, in euros. MB WAY takes the
customer's phone number as they typed it (`MbWayDetails.PhoneNumber`), and every gateway gets it in
international form (`MbWayDetails.InternationalPhoneNumber`: `912 345 678` is `+351912345678`, and a
number from elsewhere keeps its country code). Multibanco comes back with the voucher the customer pays
(`Charge.Multibanco`: `Entity`, `Reference`, `ExpiresAt`, `HostedUrl`). Both settle later, so the charge
comes back `Pending`, and its status moves through reads and notifications.

```csharp
var response = await provider.Charges.CreateAsync(new CreateChargeRequest
{
    Amount = Money.Eur(25.00m),
    Method = PaymentMethodType.MbWay,
    MbWay  = new MbWayDetails { PhoneNumber = "912 345 678" },
});
```

## Cards confirmed in the browser

A card charge with no `Card`, the customer present, is one the customer's browser confirms through the
gateway's own SDK, so no card data reaches your server. On Stripe it comes back `Pending` with
`Charge.ClientSecret`, which the page hands to Stripe.js and its Payment Element (a card, Apple Pay or Google
Pay); the outcome arrives as a notification, like any asynchronous payment.

```csharp
var created = await provider.Charges.CreateAsync(new CreateChargeRequest
{
    Amount      = Money.Eur(49.90m),
    Method      = PaymentMethodType.CreditCard,   // no Card
    ReferenceId = "order-44",
});
var clientSecret = created.Data!.ClientSecret;   // to that customer's page only
```

The client secret is on that result only: a charge read again or carried by a notification has none, and the
raw body kept for auditing has it redacted. Hand it to the customer's page, and never store or log it.

## Retries

What moves money (create, capture, cancel and refund) takes an `IdempotencyKey`, so a retry after a timeout
is answered with the first result instead of charging twice. Use one key per operation, derived from your own
id for the attempt and the operation (`attempt-42:create`, `attempt-42:refund`), and reuse it only to retry
that same request: a gateway refuses a key reused with other parameters. Set `AttemptedAt` with the key, to
the moment of the first try, and send it again with every retry, so what counts from the moment of the request
(a Pix expiry, a boleto's days) comes out the same; left out, each call counts from its own moment. Keep the
key to 64 characters, Adyen's limit. The table below says what each gateway gets. A customer's creation takes
a key too (`CustomerRequest.IdempotencyKey`): Stripe sends it, and the other gateways ignore it.

## Providers

| Package | Gateway | Idempotency key |
|---|---|---|
| `eQuantic.Payment.Stripe` | Stripe | `Idempotency-Key`, the caller's; none without one |
| `eQuantic.Payment.Pagarme` | Pagar.me | none sent |
| `eQuantic.Payment.MercadoPago` | Mercado Pago | `X-Idempotency-Key`, the caller's; without one, a fresh key, except on a Payments API capture or cancel, which carries none |
| `eQuantic.Payment.PagSeguro` | PagSeguro / PagBank | `x-idempotency-key`, the caller's or a fresh one |
| `eQuantic.Payment.Cielo` | Cielo | none sent |
| `eQuantic.Payment.Adyen` | Adyen | `Idempotency-Key`, the caller's or a fresh one |
| `eQuantic.Payment.Asaas` | Asaas | none sent |
| `eQuantic.Payment.Efi` | Efí (Gerencianet) | none sent |

Source, design notes and the other packages: [https://github.com/eQuantic/core-payment](https://github.com/eQuantic/core-payment).
