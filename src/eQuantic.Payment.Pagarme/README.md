# eQuantic.Payment.Pagarme

Pagar.me for [eQuantic.Payment](https://github.com/eQuantic/core-payment). A typed client for each Pagar.me API and the adapter that speaks eQuantic.Payment's unified model: Pix, boleto and cards, refunds and customers.

## Install

```bash
dotnet add package eQuantic.Payment.Pagarme
```

## Register

```csharp
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Pagarme;

services.AddPayments(payments => payments
    .AddPagarme(o =>
    {
        o.ApiKey  = configuration["Pagarme:ApiKey"]!;
        o.Version = PagarmeApiVersion.V5;
    }, asDefault: true));
```

The provider then answers through `IPaymentProvider`, from `IPaymentProviderFactory.Get("pagarme")` or as
the default, and every response names the version that answered.

## Versions

| Option | What it talks to |
|---|---|
| `PagarmeApiVersion.V5` (default) | Core API v5, orders and charges, Basic auth with the secret key |
| `PagarmeApiVersion.V4` | the legacy API, transactions, the key in the body |

Source and the other providers: [https://github.com/eQuantic/core-payment](https://github.com/eQuantic/core-payment).
