# eQuantic.Payment.Asaas

Asaas for [eQuantic.Payment](https://github.com/eQuantic/core-payment). A typed client for Asaas's API v3 and the adapter that speaks eQuantic.Payment's unified model: Pix, boleto and cards, refunds and customers.

## Install

```bash
dotnet add package eQuantic.Payment.Asaas
```

## Register

```csharp
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Asaas;

services.AddPayments(payments => payments
    .AddAsaas(o =>
    {
        o.ApiKey = configuration["Asaas:ApiKey"]!;
    }, asDefault: true));
```

The provider then answers through `IPaymentProvider`, from `IPaymentProviderFactory.Get("asaas")` or as
the default, and every response names the version that answered.

## Versions

| Option | What it talks to |
|---|---|
| `AsaasApiVersion.V3` (default) | API v3, decimal amounts, the `access_token` header |

## Good to know

- A charge creates its customer first, then the payment; the Pix QR code and the boleto's line are fetched right after.

Source and the other providers: [https://github.com/eQuantic/core-payment](https://github.com/eQuantic/core-payment).
