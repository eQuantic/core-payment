# eQuantic.Payment.PagSeguro

PagSeguro / PagBank for [eQuantic.Payment](https://github.com/eQuantic/core-payment). A typed client for PagSeguro's Orders API and the adapter that speaks eQuantic.Payment's unified model: Pix, boleto and cards, and refunds.

## Install

```bash
dotnet add package eQuantic.Payment.PagSeguro
```

## Register

```csharp
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.PagSeguro;

services.AddPayments(payments => payments
    .AddPagSeguro(o =>
    {
        o.Token = configuration["PagSeguro:Token"]!;
    }, asDefault: true));
```

The provider then answers through `IPaymentProvider`, from `IPaymentProviderFactory.Get("pagseguro")` or as
the default, and every response names the version that answered.

## Versions

| Option | What it talks to |
|---|---|
| `PagSeguroApiVersion.Orders` (default) | orders and charges, centavos, Bearer auth |

## Good to know

- Cards and boleto go in `charges[]` and Pix in `qr_codes[]`; a refund is a cancel with an amount.
- Creates, captures, cancels and refunds carry `x-idempotency-key`: the request's `IdempotencyKey`, or a fresh key when it has none.

Source and the other providers: [https://github.com/eQuantic/core-payment](https://github.com/eQuantic/core-payment).
