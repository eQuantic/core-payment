# eQuantic.Payment.Cielo

Cielo for [eQuantic.Payment](https://github.com/eQuantic/core-payment). A typed client for Cielo's E-commerce API 3.0 and the adapter that speaks eQuantic.Payment's unified model: cards, Pix and boleto, and refunds.

## Install

```bash
dotnet add package eQuantic.Payment.Cielo
```

## Register

```csharp
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Cielo;

services.AddPayments(payments => payments
    .AddCielo(o =>
    {
        o.MerchantId  = configuration["Cielo:MerchantId"]!;
        o.MerchantKey = configuration["Cielo:MerchantKey"]!;
    }, asDefault: true));
```

The provider then answers through `IPaymentProvider`, from `IPaymentProviderFactory.Get("cielo")` or as
the default, and every response names the version that answered.

## Versions

| Option | What it talks to |
|---|---|
| `CieloApiVersion.V3` (default) | E-commerce API 3.0: PascalCase JSON, centavos, a transactional host for writes and a query host for reads |

Source and the other providers: [https://github.com/eQuantic/core-payment](https://github.com/eQuantic/core-payment).
