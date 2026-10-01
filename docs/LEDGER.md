# Ledger

What happened in this repository, one line per event, oldest first. Each line cites the issue the
event closed or, for what came before issues were kept here, its pull request or commits. What each
release holds is in [`CHANGELOG.md`](../CHANGELOG.md).

- 2026-07-21 · The unified payment abstraction, with Pagar.me and Stripe, then Mercado Pago, then Cielo, PagSeguro, Adyen, Efí and Asaas (`ca753c9`, `5660c5c`, `4d25304`)
- 2026-09-30 · CI on GitHub Actions builds, tests and packs, and every package carries its own README and the eQuantic icon (#1)
- 2026-09-30 · A merge to `main` releases through semantic-release, and 1.0.0 is on nuget.org (#2)
- 2026-10-01 · Verified gateway notifications, Stripe's signature first, released as 1.1.0 (#7, eQuantic/equantic-subscription-api#117)
- 2026-10-01 · The caller's idempotency key on create, capture, cancel and refund, released as 1.2.0 (#8, eQuantic/equantic-subscription-api#60)
- 2026-10-01 · Saved cards charged with the customer away, customers kept in step, and boleto, on Stripe, released as 1.3.0 (#9, eQuantic/equantic-subscription-api#133; part of eQuantic/equantic-subscription-api#118 and eQuantic/equantic-subscription-api#119)
- 2026-10-01 · The working agreement lives in the repository: the Workflow section of `CLAUDE.md` and `AGENTS.md`, the session-start hook, and planning in the central OpenSpec store, eQuantic/equantic-specs (#10)
