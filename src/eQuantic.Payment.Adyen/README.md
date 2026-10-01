# eQuantic.Payment.Adyen

Adyen for [eQuantic.Payment](https://github.com/eQuantic/core-payment). A typed client for Adyen's Checkout API and the adapter that speaks eQuantic.Payment's unified model: payments, their modifications and refunds.

## Install

```bash
dotnet add package eQuantic.Payment.Adyen
```

## Register

```csharp
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Adyen;

services.AddPayments(payments => payments
    .AddAdyen(o =>
    {
        o.ApiKey          = configuration["Adyen:ApiKey"]!;
        o.MerchantAccount = configuration["Adyen:MerchantAccount"]!;
    }, asDefault: true));
```

The provider then answers through `IPaymentProvider`, from `IPaymentProviderFactory.Get("adyen")` or as
the default, and every response names the version that answered.

## Versions

| Option | What it talks to |
|---|---|
| `AdyenApiVersion.V71` (default) | Checkout API `/v71`, minor units, `X-API-Key` and the merchant account |

## Good to know

- Capture, cancel and refund answer `received`; their final state arrives by webhook. A refusal is HTTP 200 with `resultCode: Refused`.
- The default host is Adyen's test environment; set `BaseUrl` for live.
- Every POST carries `Idempotency-Key`: the request's `IdempotencyKey` (at most 64 characters), or a fresh key when it has none. Without a `ReferenceId`, the payment's `reference` is the idempotency key, so a retry sends the same one.

Source and the other providers: [https://github.com/eQuantic/core-payment](https://github.com/eQuantic/core-payment).
