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

- **Contracts**: `IPaymentProvider` with its `Charges`, `Refunds`, `Customers` and `Notifications`
  operations, and `IPaymentProviderFactory`, which resolves a provider by name, by name and version, or the
  default. `Notifications.Verify` checks that a webhook came from the gateway before parsing it; a provider
  that does not verify its notifications yet answers with a failure.
- **The unified model**: `CreateChargeRequest`, `CaptureRequest`, `CancelRequest`, `RefundRequest`,
  `CustomerRequest`; `Charge`, `Customer`, `Refund`, with the Pix, boleto and card details each method
  returns; `Money` in centavos.
- **`PaymentResponse<T>`**: the result or a `PaymentError`, the provider and version that answered, and
  the gateway's raw body for auditing.
- **The registration**: `AddPayments(...)` and the `PaymentBuilder` the provider packages extend.

## Retries

What moves money (create, capture, cancel and refund) takes an `IdempotencyKey`, so a retry after a timeout
is answered with the first result instead of charging twice. Use one key per operation, derived from your own
id for the attempt and the operation (`attempt-42:create`, `attempt-42:refund`), and reuse it only to retry
that same request: a gateway refuses a key reused with other parameters. Keep it to 64 characters, Adyen's
limit. The table below says what each gateway gets.

## Providers

| Package | Gateway | Idempotency key |
|---|---|---|
| `eQuantic.Payment.Stripe` | Stripe | `Idempotency-Key`, the caller's; none without one |
| `eQuantic.Payment.Pagarme` | Pagar.me | none sent |
| `eQuantic.Payment.MercadoPago` | Mercado Pago | `X-Idempotency-Key`, the caller's; without one, a fresh key on create and refund and none on capture and cancel |
| `eQuantic.Payment.PagSeguro` | PagSeguro / PagBank | `x-idempotency-key`, the caller's or a fresh one |
| `eQuantic.Payment.Cielo` | Cielo | none sent |
| `eQuantic.Payment.Adyen` | Adyen | `Idempotency-Key`, the caller's or a fresh one |
| `eQuantic.Payment.Asaas` | Asaas | none sent |
| `eQuantic.Payment.Efi` | Efí (Gerencianet) | none sent |

Source, design notes and the other packages: [https://github.com/eQuantic/core-payment](https://github.com/eQuantic/core-payment).
