# eQuantic.Payment

Unified payment-gateway abstraction for the Brazilian market, on **.NET 10**.

Each gateway has its own client package (`eQuantic.Payment.Pagarme`, `eQuantic.Payment.Stripe`, …), **structured by API version** — because `x.x` behaves one way and `x.y` another. On top of them all, the `eQuantic.Payment` package exposes **a single output**: the same request, the same response, always carrying `provider` + `version`.

```
┌─────────────────────────────────────────────────────────────┐
│  Your application  →  IPaymentProviderFactory / IPaymentProvider │  ← single contract
└─────────────────────────────────────────────────────────────┘
              │ eQuantic.Payment (core: models + DI + factory)
   ┌──────────┴──────────────────────────────────────────────┐
   ▼                                                           ▼
 8 provider packages, each structured by version:      adapters → unified
   Pagar.me (v4/v5)   ·  Stripe (dated Stripe-Version)         PaymentResponse<T>
   Mercado Pago (payments/orders)  ·  PagSeguro (orders)
   Cielo (3.0)  ·  Adyen (v71)  ·  Efí (pix/cobrancas)  ·  Asaas (v3)
```

## Packages

Each package ships on NuGet with its own README and the eQuantic icon:

| Package | What it holds |
|---|---|
| `eQuantic.Payment` | the contracts, the unified model, the factory and the registration |
| `eQuantic.Payment.Stripe` | Stripe |
| `eQuantic.Payment.Pagarme` | Pagar.me |
| `eQuantic.Payment.MercadoPago` | Mercado Pago |
| `eQuantic.Payment.PagSeguro` | PagSeguro / PagBank |
| `eQuantic.Payment.Cielo` | Cielo |
| `eQuantic.Payment.Adyen` | Adyen |
| `eQuantic.Payment.Asaas` | Asaas |
| `eQuantic.Payment.Efi` | Efí (Gerencianet) |

```bash
dotnet add package eQuantic.Payment.Stripe
```

## Building and releasing

CI runs on GitHub Actions for every pull request to `main` and every push to it
(`.github/workflows/ci.yml`): the build with warnings as errors, the tests, and a pack that checks every
package carries its README and the icon.

A merge is a release. Every push to `main` builds and tests again, then semantic-release reads the commit
messages since the last tag (`.github/workflows/release.yml`, `release.config.mjs`): `✨ feat` is a minor,
`🐛 fix` and `⚡ perf` a patch, a `!` or a `BREAKING CHANGE` a major, and anything else releases nothing.
When there is a release, it writes `CHANGELOG.md`, tags `vX.Y.Z`, pushes every package with its symbols to
nuget.org with the organization's `NUGET_KEY` secret, creates the GitHub release, and commits the version
back to `Directory.Build.props` with `[skip ci]`. A push to a `preview` branch releases `X.Y.Z-preview.N`.

## Solution layout

```
src/
├── eQuantic.Payment/                 # Core: contracts, unified models, factory, DI
│   ├── Abstractions/                 # IPaymentProvider, IChargeOperations, IRefundOperations,
│   │                                 #   ICustomerOperations, INotificationOperations,
│   │                                 #   IPaymentProviderFactory
│   ├── Models/                       # Money, PaymentStatus, PaymentMethodType, ProviderInfo,
│   │   ├── Requests/                 #   CreateChargeRequest, CaptureRequest, CancelRequest, RefundRequest,
│   │   │                             #   CustomerRequest, AddressRequest
│   │   └── Results/                  #   Charge, Customer, Refund (+ Pix/Boleto/Card outputs)
│   ├── Http/                         # PaymentHttpClientBase, ApiResult<T>
│   └── DependencyInjection/          # AddPayments(...), PaymentBuilder, PaymentRegistry, factory
│
├── eQuantic.Payment.Pagarme/         # Pagar.me client
│   ├── V4/                           # Legacy API (transactions) — https://api.pagar.me/1
│   │   ├── Models/                   #   faithful wire DTOs
│   │   └── Mapping/                  #   eQuantic.Mapper IMapper<,> implementations
│   ├── V5/                           # Core API v5 (orders/charges) — https://api.pagar.me/core/v5
│   │   ├── Models/                   #   faithful wire DTOs
│   │   └── Mapping/                  #   eQuantic.Mapper IMapper<,> implementations
│   └── PagarmeServiceCollectionExtensions.cs   # AddPagarme(...)
│
├── eQuantic.Payment.Stripe/          # Stripe client
│   ├── V1/                           # REST v1 (PaymentIntents) — cards, PIX, boleto
│   │   ├── Models/                   #   faithful wire DTOs (+ expandable converter)
│   │   └── Mapping/                  #   eQuantic.Mapper IMapper<,> implementations
│   └── StripeServiceCollectionExtensions.cs    # AddStripe(...)
│
├── eQuantic.Payment.MercadoPago/     # Mercado Pago client
│   ├── Payments/                     # Payments API (/v1/payments) — flat, numeric ids, decimal amounts
│   │   ├── Models/ + Mapping/
│   ├── Orders/                       # Orders API (/v1/orders) — nested, string ids, string amounts
│   │   ├── Models/ + Mapping/
│   ├── Customers/                    # shared /v1/customers (both API versions)
│   └── MercadoPagoServiceCollectionExtensions.cs   # AddMercadoPago(...)
│
├── eQuantic.Payment.PagSeguro/       # PagSeguro/PagBank — Orders/Charges API (Orders/ folder)
├── eQuantic.Payment.Cielo/           # Cielo — E-commerce API 3.0 (V3/ folder, PascalCase JSON, 2 hosts)
├── eQuantic.Payment.Adyen/           # Adyen — Checkout API (V71/ folder, async modifications)
├── eQuantic.Payment.Asaas/           # Asaas — API v3 (V3/ folder, decimal money, real customers)
└── eQuantic.Payment.Efi/             # Efí/Gerencianet — Pix API (mTLS) + Cobranças API (Pix/ + Cobrancas/)
```

Every provider package follows the same internal layout as MercadoPago (`Models/` faithful DTOs, `Mapping/` eQuantic.Mapper `IMapper<,>` classes, client, operations, provider adapter, `Add<Name>` DI extension).

Each provider version has its own **typed client** (speaking that version's native wire format) and **adapters** that translate to the unified model. Switching version does not change a single line of the code that consumes `IPaymentProvider`.

## How versioning is modeled per provider

| Provider | Version axis | How versions differ |
|----------|--------------|---------------------|
| **Pagar.me** | `V4` / `V5` (enum) | Different URLs and resources: v4 is *transaction-based* (`/transactions`, auth via `api_key` in the body); v5 is *order/charge-based* (`/orders` + `/charges`, Basic auth with a secret key). |
| **Stripe** | dated `Stripe-Version` (`2024-06-20`, `2025-04-30.basil`) | Same REST `v1`, but the version pinned in the header changes the response shape. Sent as form-urlencoded, Bearer auth. |
| **Mercado Pago** | `Payments` / `Orders` (enum) | Two parallel APIs: `Payments` (`/v1/payments`) is flat with numeric ids and **decimal** amounts; `Orders` (`/v1/orders`) is the newer unified model with string ids, **string** amounts and a different status vocabulary. Bearer auth + `X-Idempotency-Key`. |
| **PagSeguro / PagBank** | `Orders` (enum) | Modern Orders/Charges API (`/orders` + `/charges`), centavos, Bearer auth. Card/boleto go in `charges[]`, PIX in `qr_codes[]`; refund = cancel with amount. (Legacy XML API is a separate axis, not modeled.) |
| **Cielo** | `3.0` (enum `V3`) | E-commerce API 3.0, **PascalCase** JSON, centavos, dual-header auth (`MerchantId`/`MerchantKey`), **two hosts** (transactional for POST/PUT, query for GET). Numeric status codes. |
| **Adyen** | dated path `v71` (enum) | Checkout API `/v71/payments`; minor-unit amounts; `X-API-Key` + `merchantAccount`. Async modifications (capture/cancel/refund return `received`, final state via webhook); a refusal is HTTP 200 with `resultCode: Refused`. |
| **Efí / Gerencianet** | `Pix` / `Cobrancas` (enum) | Two disjoint APIs as versions: `pix` (Central Bank Pix, decimal-string money, **OAuth2 + mTLS certificate**) handles PIX; `cobrancas` (centavos, OAuth2) handles boleto + card. |
| **Asaas** | `v3` (enum) | API v3, **decimal** money, `access_token` header. Standalone customers (`/customers`); a charge first creates the customer, then the payment; PIX QR and boleto line are fetched via follow-up GETs. |

The version identifier always appears in the output (`response.Provider.Version`) and in the registration key (`pagarme@v5`, `stripe@2025-04-30.basil`, `mercadopago@payments`).

> **Amounts:** the unified `Money` is centavos-based, but each adapter converts to the gateway's format — Pagar.me/Stripe use centavos, **Mercado Pago uses decimal reais** (Payments API) or **decimal strings** (Orders API). **Cards on Mercado Pago** require `Card.Brand` (used as the MP `payment_method_id`, e.g. `visa`/`debvisa`); Pagar.me and Stripe infer the brand from the token/PAN.

## Faithful wire models (Anti-Corruption Layer)

Each provider version has **wire DTOs that mirror the real API response 1:1** (`V5/Models`, `V4/Models`, `V1/Models`), transcribed from the official documentation — including fields we do not consume yet. Only afterward do the mappers convert those faithful DTOs into the unified model. This isolates gateway changes and prevents a misread field from becoming a silent bug.

Rules this layer follows (validated against the official docs):
- **Never** model partially "from memory". Example: Stripe removed the `charges` array in API version `2022-11-15` — the correct field is `latest_charge` (an expandable id), which requires `expand[]=latest_charge`. Modeling `charges[]` would leave card data always null.
- Serialization uses `SnakeCaseLower`; only digit-suffixed keys (`line_1`/`line_2`) need an explicit `[JsonPropertyName]`.
- Status enums map with an `Unknown` catch-all — a new gateway value never breaks deserialization.
- The raw body is always preserved in `PaymentResponse.RawResponse` for auditing.

> **Stripe boleto** requires `billing_details` with a full address — that is why `CustomerRequest.Address` is required in that case. Stripe boleto expiry is in days (`expires_after_days`, 0–60), not a timestamp.

## Mapping (eQuantic.Mapper)

All mapping goes through [eQuantic.Mapper](https://www.nuget.org/packages/eQuantic.Mapper/). Each provider ships `IMapper<TSource, TDestination>` implementations under its `Mapping/` folder, registered via `AddMappers(...)` and resolved through `IMapperFactory`. The operations classes never map by hand — they resolve the mapper from the factory and call `Map(...)`.

This covers both directions:
- **Response → unified model**: e.g. `V5ChargeMapper : IMapper<V5ChargeResponse, Charge>`, `StripeChargeMapper : IMapper<StripePaymentIntent, Charge>`.
- **Unified request → provider wire**: Pagar.me maps to request DTOs (`IMapper<CreateChargeRequest, V5OrderRequest>`); Stripe maps to a form body (`IMapper<CreateChargeRequest, StripeForm>`, where `StripeForm` is the list of urlencoded pairs). The Stripe PaymentIntent form mapper is a **context mapper** (`IMapper<CreateChargeRequest, StripeForm, StripeRequestContext>`) because boleto expiry (`expires_after_days`) needs a reference "today".

Only two things stay outside `IMapper`, because they are not object-to-object mappings: error-envelope parsing (`PagarmeErrorMapper` / `StripeErrorMapper`) and appending a single scalar endpoint parameter (e.g. Stripe capture's `amount_to_capture`).

## Installation & configuration (DI)

```csharp
using eQuantic.Payment.DependencyInjection;
using eQuantic.Payment.Pagarme;
using eQuantic.Payment.Stripe;

services.AddPayments(payments => payments
    .AddPagarme(o =>
    {
        o.ApiKey  = builder.Configuration["Pagarme:ApiKey"]!;
        o.Version = PagarmeApiVersion.V5;      // or V4
    }, asDefault: true)
    .AddStripe(o =>
    {
        o.ApiKey  = builder.Configuration["Stripe:ApiKey"]!;
        o.Version = StripeApiVersion.V2025_04_30_Basil;
    })
    .AddMercadoPago(o =>
    {
        o.AccessToken = builder.Configuration["MercadoPago:AccessToken"]!;
        o.Version     = MercadoPagoApiVersion.Payments;   // or Orders
    })
    .AddPagSeguro(o => { o.Token = builder.Configuration["PagSeguro:Token"]!; })
    .AddCielo(o =>
    {
        o.MerchantId  = builder.Configuration["Cielo:MerchantId"]!;
        o.MerchantKey = builder.Configuration["Cielo:MerchantKey"]!;
    })
    .AddAdyen(o =>
    {
        o.ApiKey          = builder.Configuration["Adyen:ApiKey"]!;
        o.MerchantAccount = builder.Configuration["Adyen:MerchantAccount"]!;
    })
    .AddAsaas(o => { o.ApiKey = builder.Configuration["Asaas:ApiKey"]!; })
    .AddEfi(o =>
    {
        o.ClientId     = builder.Configuration["Efi:ClientId"]!;
        o.ClientSecret = builder.Configuration["Efi:ClientSecret"]!;
        o.Version      = EfiApiVersion.Cobrancas;         // or Pix (requires o.Certificate + o.PixKey)
    }));
```

> **Partial support is explicit.** Not every gateway exposes every operation. Providers with no standalone customer resource (PagSeguro, Cielo, Adyen, Efí) return a clear "unsupported" error from `Customers.*` (via `UnsupportedCustomerOperations`); Asaas and the others implement it. Each Efí/Mercado Pago version only supports the payment methods its API covers (e.g. `efi@pix` rejects boleto/card). These come back as a failed `PaymentResponse` with an explanatory message, never an exception.
>
> **Efí Pix requires mTLS:** set `EfiOptions.Certificate` (the `.p12` from your Efí account) and `EfiOptions.PixKey`; the handler attaches the client certificate to every Pix call.

`AddPagarme`/`AddStripe` also register their `eQuantic.Mapper` mappers, so no extra `AddMappers()` call is needed. You can register **the same provider in multiple versions** at once — each becomes an independent `name@version` entry.

## Usage — the output is always the same

```csharp
public class CheckoutService(IPaymentProviderFactory payments)
{
    public async Task<string> ChargePixAsync()
    {
        // Resolve the provider: default, by name, or by name + version
        IPaymentProvider provider = payments.GetDefault();
        // = payments.Get("pagarme");
        // = payments.Get("stripe", "2025-04-30.basil");

        var response = await provider.Charges.CreateAsync(new CreateChargeRequest
        {
            Amount      = Money.Brl(150.00m),
            Method      = PaymentMethodType.Pix,
            ReferenceId = "order-42",
            Customer    = new CustomerRequest
            {
                Name = "João Silva", Email = "joao@ex.com", Document = "123.456.789-09",
            },
        });

        // response.Provider  → { Name = "pagarme", Version = "v5" }  (or stripe@..., etc)
        if (!response.Success)
            throw new Exception(response.Error!.Message);

        var charge = response.Data!;
        // charge.Status (unified enum), charge.Pix.QrCode, charge.Pix.QrCodeImageUrl, ...
        return charge.Pix!.QrCode!;
    }
}
```

The same code works for **card** (`PaymentMethodType.CreditCard` + `Card`), **boleto** (`PaymentMethodType.Boleto` + `Boleto`) and **PIX**, on any provider/version. Only the `AddPayments` configuration changes.

### Available operations (`IPaymentProvider`)

- `Charges.CreateAsync` / `GetAsync` / `CaptureAsync` / `CancelAsync`
- `Refunds.CreateAsync`
- `Customers.CreateAsync` / `GetAsync`

They all return `PaymentResponse<T>` with `Provider`, `Success`, `Data`, `Error` and `RawResponse` (the raw provider body, preserved for auditing).

## Adding a new provider

1. **Consult the official API documentation** and transcribe the faithful schemas (request + response) into DTOs under `Models/` — 1:1, before any mapping.
2. Create `eQuantic.Payment.<Name>` referencing `eQuantic.Payment` and `eQuantic.Mapper`.
3. For each API version: a typed client (deriving from `PaymentHttpClientBase`), `IMapper<,>` mappers (wire DTO ↔ unified model) under `Mapping/`, and an adapter implementing `IPaymentProvider`.
4. Expose an `Add<Name>(this PaymentBuilder, ...)` extension that registers the `HttpClient`, calls `AddMappers(o => o.FromAssembly(...))`, and calls `builder.AddProvider(info, factory)`.

## Build & tests

```bash
dotnet build          # net10.0, nullable + warnings-as-errors
dotnet test           # mapping, factory and value-object tests
```
