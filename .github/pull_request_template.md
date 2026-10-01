<!--
  In English, with a title that is `emoji type: description` (✨ feat, 🐛 fix, 📝 docs, 🔧 chore,
  ♻️ refactor, ✅ test, 👷 ci, ⚡ perf, 📦 build): the squash merge makes it the commit message, and
  semantic-release reads its type. No attribution to Claude anywhere in this body.
-->

## What

<!-- What changes, in the terms of a developer using the packages: the call they make, the option
     they set, what they get back. Name the packages whose public API moves, and whether the change
     breaks a consumer. -->

## Why

Closes #

<!-- The board issue this pull request closes. A change that creates or alters behaviour links its
     OpenSpec change by its absolute URL,
     https://github.com/eQuantic/equantic-specs/tree/main/openspec/changes/core-<what-it-delivers>,
     and quotes the requirements it implements: the store is private, so a reviewer cannot read
     them there. A refactor, tooling, docs or CI change has no OpenSpec change. -->

## Proof

<!-- What shows it works: the build, the tests (how many, which are new), a run against the
     gateway's sandbox or mock, the packages. And what the merge releases: ✨ feat a minor, 🐛 fix
     or ⚡ perf a patch, a ! or a BREAKING CHANGE a major, anything else nothing. -->

## Checklist

- [ ] The branch is `<type>/<slug>`, and the title is `emoji type: description`
- [ ] The docs the change touches are current: the root README, each package's README, `CLAUDE.md`
- [ ] `docs/LEDGER.md` has its line, citing the issue
- [ ] The OpenSpec change, if there is one, is linked above, and gets archived in the store once this merges
- [ ] Copilot's review is requested, and every finding is fixed or answered in its thread
