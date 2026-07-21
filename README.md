# eQuantic.Payment

Unified payment-gateway abstraction for the Brazilian market, on **.NET 10**.

Each gateway has its own client package (`eQuantic.Payment.Pagarme`, `eQuantic.Payment.Stripe`, …), **structured by API version** — because `x.x` behaves one way and `x.y` another. On top of them all, the `eQuantic.Payment` package exposes **a single output**: the same request, the same response, always carrying `provider` + `version`.

```
┌─────────────────────────────────────────────────────────────┐
│  Your application  →  IPaymentProviderFactory / IPaymentProvider │  ← single contract
└─────────────────────────────────────────────────────────────┘
              │ eQuantic.Payment (core: models + DI + factory)
   ┌──────────┼───────────────────────────┐
   ▼          ▼                            ▼
Pagar.me    Stripe                      (next)
 ├ V4  ──┐    └ V1 (dated Stripe-Version:  MercadoPago, Cielo, …
 └ V5  ──┤       2024-06-20 /
         │       2025-04-30.basil)
   adapters → unified PaymentResponse<T>
```

## Solution layout

```
src/
├── eQuantic.Payment/                 # Core: contracts, unified models, factory, DI
│   ├── Abstractions/                 # IPaymentProvider, IChargeOperations, IRefundOperations,
│   │                                 #   ICustomerOperations, IPaymentProviderFactory
│   ├── Models/                       # Money, PaymentStatus, PaymentMethodType, ProviderInfo,
│   │   ├── Requests/                 #   CreateChargeRequest, CustomerRequest, AddressRequest, RefundRequest
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
└── eQuantic.Payment.Stripe/          # Stripe client
    ├── V1/                           # REST v1 (PaymentIntents) — cards, PIX, boleto
    │   ├── Models/                   #   faithful wire DTOs (+ expandable converter)
    │   └── Mapping/                  #   eQuantic.Mapper IMapper<,> implementations
    └── StripeServiceCollectionExtensions.cs    # AddStripe(...)
```

Each provider version has its own **typed client** (speaking that version's native wire format) and **adapters** that translate to the unified model. Switching version does not change a single line of the code that consumes `IPaymentProvider`.

## How versioning is modeled per provider

| Provider | Version axis | How versions differ |
|----------|--------------|---------------------|
| **Pagar.me** | `V4` / `V5` (enum) | Different URLs and resources: v4 is *transaction-based* (`/transactions`, auth via `api_key` in the body); v5 is *order/charge-based* (`/orders` + `/charges`, Basic auth with a secret key). |
| **Stripe** | dated `Stripe-Version` (`2024-06-20`, `2025-04-30.basil`) | Same REST `v1`, but the version pinned in the header changes the response shape. Sent as form-urlencoded, Bearer auth. |

The version identifier always appears in the output (`response.Provider.Version`) and in the registration key (`pagarme@v5`, `stripe@2025-04-30.basil`).

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
    }));
```

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
