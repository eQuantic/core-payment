# eQuantic.Payment.MercadoPago

Mercado Pago for [eQuantic.Payment](https://github.com/eQuantic/core-payment). A typed client for Mercado Pago's two payment APIs and the adapter that speaks eQuantic.Payment's unified model: Pix, boleto and cards, refunds and customers.

## Install

```bash
dotnet add package eQuantic.Payment.MercadoPago
```

## Register

```csharp
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.MercadoPago;

services.AddPayments(payments => payments
    .AddMercadoPago(o =>
    {
        o.AccessToken = configuration["MercadoPago:AccessToken"]!;
        o.Version     = MercadoPagoApiVersion.Payments;
    }, asDefault: true));
```

The provider then answers through `IPaymentProvider`, from `IPaymentProviderFactory.Get("mercadopago")` or as
the default, and every response names the version that answered.

## Versions

| Option | What it talks to |
|---|---|
| `MercadoPagoApiVersion.Payments` (default) | `/v1/payments`: numeric ids, decimal amounts |
| `MercadoPagoApiVersion.Orders` | `/v1/orders`: string ids, string amounts, its own statuses |

## Good to know

- Cards need `CardDetails.Brand`, which Mercado Pago takes as the `payment_method_id` (`visa`, `debvisa`…).

Source and the other providers: [https://github.com/eQuantic/core-payment](https://github.com/eQuantic/core-payment).
