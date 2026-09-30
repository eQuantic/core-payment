# eQuantic.Payment.Efi

Efí (Gerencianet) for [eQuantic.Payment](https://github.com/eQuantic/core-payment). Typed clients for Efí's Pix and Cobranças APIs and the adapter that speaks eQuantic.Payment's unified model.

## Install

```bash
dotnet add package eQuantic.Payment.Efi
```

## Register

```csharp
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Efi;

services.AddPayments(payments => payments
    .AddEfi(o =>
    {
        o.ClientId     = configuration["Efi:ClientId"]!;
        o.ClientSecret = configuration["Efi:ClientSecret"]!;
        o.Version      = EfiApiVersion.Pix;
        o.Certificate  = certificate;           // the mTLS client certificate the Pix API requires
        o.PixKey       = configuration["Efi:PixKey"];
    }, asDefault: true));
```

The provider then answers through `IPaymentProvider`, from `IPaymentProviderFactory.Get("efi")` or as
the default, and every response names the version that answered.

## Versions

| Option | What it talks to |
|---|---|
| `EfiApiVersion.Pix` (default) | the Central Bank's Pix API: OAuth2 over an mTLS client certificate, decimal-string amounts |
| `EfiApiVersion.Cobrancas` | boleto and cards: OAuth2, centavos |

## Good to know

- `Sandbox = true` points either API at Efí's homologation hosts.

Source and the other providers: [https://github.com/eQuantic/core-payment](https://github.com/eQuantic/core-payment).
