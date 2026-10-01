# eQuantic.Payment

A unified payment-gateway abstraction for the Brazilian market, on .NET 10. The `eQuantic.Payment`
package holds the contracts, the unified model, the factory and the registration; each gateway has
its own package, structured by API version: Pagar.me, Stripe, Mercado Pago, PagSeguro, Cielo, Adyen,
Asaas and Efí. The [README](README.md) is the guide to the design (the faithful wire models, the
mappers, how each gateway's versions are modelled, how to add a provider), and every package
documents itself in its own `README.md`, which is also its page on nuget.org.

## Build and test

```bash
dotnet build          # net10.0, nullable, warnings as errors
dotnet test           # the providers against stubbed HTTP, the mappers, the factory, the guards
dotnet pack -o artifacts
```

The SDK is one that `global.json` accepts (10.0.100 or a later 10.0 feature band); in a cloud
container the session-start hook installs it. CI (`.github/workflows/ci.yml`) builds, tests and packs
every pull request and every push to `main`, and a merge to `main` is a release
(`.github/workflows/release.yml`, `release.config.mjs`): semantic-release publishes every package to
nuget.org at the version the commit messages since the last tag call for, and writes `CHANGELOG.md`
and the `<Version>` in `Directory.Build.props` back to `main`.

## Workflow

The working agreement for every session in this repository. It is the same text in `CLAUDE.md` and
`AGENTS.md`, and a test (`WorkflowSectionTests`) fails when the two differ.

- **Language.** The conversation with the owner, in the chat, is in Brazilian Portuguese. Everything
  committed or posted is in English: code comments, XML documentation, Markdown, commit messages,
  issues, pull requests and review replies. Never comment code in Portuguese.
- **Branches.** Always `<type>/<slug>`, with the type one of `feat`, `fix`, `chore`, `refactor`,
  `docs`, `test`, `ci`, `perf` or `build`, cut from `origin/main`. Never the `claude/...` branch a
  session suggests. Nothing goes straight to `main`: every change reaches it through a pull request.
- **Commits.** In English, as `emoji type: description`: `✨ feat:`, `🐛 fix:`, `📝 docs:`,
  `🔧 chore:`, `♻️ refactor:`, `✅ test:`, `👷 ci:`, `⚡ perf:`, `📦 build:`. A commit never carries a
  co-authorship or attribution line for Claude: no `Co-Authored-By`, no session link.
  `.claude/settings.json` turns off every attribution Claude Code would add.
- **Identity.** The repository commits as `Edgar Mesquita <edgar@equantic.tech>`. In a web session,
  the session-start hook sets that identity in the repository's git config on every new container
  and turns off commit and tag signing there, since the container's signing key is not the owner's.
- **Issues.** Every change has an issue on the GitHub Project board,
  [#16](https://github.com/orgs/eQuantic/projects/16). When there is none, create it as a sub-issue
  of its epic or feature, with the right type (Epic, Feature, User Story, Task or Bug), and add it
  to the board.
- **Pull requests.** In English, following `.github/pull_request_template.md`. The pull request
  closes its issue (`Closes #N`) and carries no attribution to Claude: when a tool appends a
  "Generated with…" footer, remove it.
- **Review and merge.** Always request GitHub Copilot's review, and wait for it. When it has
  findings, fix each one or answer in its thread why not, request the review again, and repeat until
  a round brings nothing new. Then, with CI green, squash-merge, with the pull request's title as
  the commit message. That title is what semantic-release reads: `✨ feat` releases a minor, `🐛 fix`
  and `⚡ perf` a patch, a `!` or a `BREAKING CHANGE` a major, and anything else releases nothing.
- **CI.** GitHub Actions runs `.github/workflows/`. While GitHub Actions has no credits, CI runs on
  eQuantic Space: follow the pipelines on GitHub Actions, and once the credits are exhausted follow
  them with `eqs runs ls`, `eqs runs get <n>` and `eqs runs logs <n>`. The environment has
  `EQS_API_URL` and `EQS_TOKEN`, and the environment's setup script installs `eqs`.
- **Documentation.** Kept current in the same pull request as the change: the root `README.md`, the
  `README.md` of every package the change touches (it is the package's page on nuget.org), and this
  file. The history is `docs/LEDGER.md`: one line per event, citing its issue. `CHANGELOG.md` is
  written by the release, never by hand.
- **OpenSpec.** The specs and the changes live in the central store,
  [eQuantic/equantic-specs](https://github.com/eQuantic/equantic-specs), as the
  [`core` workstream](https://github.com/eQuantic/equantic-specs/blob/main/workstreams/core.md):
  specs under `openspec/specs/core/`, changes under `openspec/changes/`, each named
  `core-<what-it-delivers>`. The store's `CLAUDE.md` is how a change flows, and its
  `openspec/config.yaml` and `workstreams/core.md` hold the context and the rules every artifact is
  written against. Here `openspec/config.yaml` only points there (`store: equantic-specs`), so
  `/opsx:propose`, `/opsx:apply` and the `openspec` CLI act on the store. Never create
  `openspec/specs` or `openspec/changes` here: a local planning root would quietly take this
  repository off the store, and CI's `openspec` job fails on either.
  - Pull the store before starting: `git -C ../equantic-specs pull --rebase`.
  - A change that creates or alters behaviour starts as a proposal (`/opsx:propose`) in the store,
    committed and pushed there at once (`📝 docs: propose <change>`), before the code. A refactor,
    tooling, docs or CI change needs no proposal.
  - Implement with `/opsx:apply <change>`, always naming the change: the store holds every
    product's changes, and an apply with no name can pick another workstream's. The pull request
    links the change by its absolute URL and quotes the requirements it implements, since the store
    is private and its reviewers cannot read it.
  - Once every pull request the change lists has merged, archive it in the store (`/opsx:archive`),
    so the store's `openspec/specs` describes what is on `main`.
  - A capability gets its spec the first time a change touches it, never before.
  - The CLI is pinned by `tools/openspec/package-lock.json` to the store's version, and runs with
    telemetry off (`OPENSPEC_TELEMETRY=0`). The store's CI runs `openspec validate --all --strict`
    on every push; this repository's checks the pointer.
- **Sessions.** `.claude/settings.json` registers `.claude/hooks/session-start.sh`. In a web session
  it prepares the container: the owner's git identity with signing off, the .NET SDK, Docker, the
  OpenSpec CLI, and the store cloned beside the repository and registered. On a laptop it only
  installs the pinned OpenSpec CLI under `tools/openspec` and puts it on the session's PATH, and
  warns when the store is not registered; the identity, the signing, the SDKs, Docker and the store
  registry stay as their owner set them. What it cannot prepare it tells the person, and the session
  still starts. Every installer it downloads is pinned by version and SHA-256, and CI's
  `session-start` job runs it the way a fresh container would.
