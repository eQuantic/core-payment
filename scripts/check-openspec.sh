#!/usr/bin/env bash
# This repository plans in the central OpenSpec store, eQuantic/equantic-specs. The check fails, naming
# the problem and the store, when:
#
#   - openspec/config.yaml declares anything but `store: equantic-specs` (comments and blank lines
#     aside): a pointer's own context and rules are ignored, and another id is another store;
#   - openspec/specs or openspec/changes exists: the nearest real planning root wins over the pointer,
#     so either would quietly take the repository off the store;
#   - tools/openspec/package-lock.json pins an OpenSpec CLI older than 1.14.0, the first whose apply,
#     started in a repository that points at a store, edits that repository as well as the store.
#
# It reads files only and runs no OpenSpec CLI: the store is private, and CI cannot reach it.
#
#   scripts/check-openspec.sh [repository]   checks the repository (this checkout by default)
#   scripts/check-openspec.sh --self-test    proves the check both ways on fixture trees
set -uo pipefail

STORE_ID='equantic-specs'
STORE='eQuantic/equantic-specs'
CLI_FLOOR='1.14.0'

# 0 when $1 >= $2, for plain x.y.z versions.
version_at_least() {
  local -a have need
  local i
  IFS=. read -r -a have <<<"$1"
  IFS=. read -r -a need <<<"$2"
  for i in 0 1 2; do
    if ((${have[i]:-0} > ${need[i]:-0})); then return 0; fi
    if ((${have[i]:-0} < ${need[i]:-0})); then return 1; fi
  done
  return 0
}

check() {
  local root=$1 config="$1/openspec/config.yaml" lock="$1/tools/openspec/package-lock.json"
  local failed=0 declarations folder pinned

  if [ ! -f "$config" ]; then
    echo "::error file=openspec/config.yaml::openspec/config.yaml is missing; it must point at the central store with 'store: $STORE_ID' ($STORE)"
    failed=1
  else
    declarations="$(grep -vE '^[[:space:]]*(#|$)' "$config")"
    if ! printf '%s\n' "$declarations" | grep -qxE "store:[[:space:]]*[\"']?${STORE_ID}[\"']?[[:space:]]*(#.*)?" ||
      [ "$(printf '%s\n' "$declarations" | wc -l | tr -d ' ')" != "1" ]; then
      echo "::error file=openspec/config.yaml::openspec/config.yaml must declare 'store: $STORE_ID' and nothing else, since the context and the rules are the store's ($STORE); it declares:"
      printf '%s\n' "$declarations" | sed 's/^/    /'
      failed=1
    fi
  fi

  for folder in specs changes; do
    if [ -e "$root/openspec/$folder" ]; then
      echo "::error file=openspec/$folder::openspec/$folder exists, and a planning root here wins over the pointer; it belongs in $STORE, under openspec/$folder/ (the core workstream)"
      failed=1
    fi
  done

  pinned="$(sed -n '/"node_modules\/@fission-ai\/openspec"/,/}/s/^[[:space:]]*"version":[[:space:]]*"\([^"]*\)".*/\1/p' "$lock" 2>/dev/null | head -n 1)"
  if [ -z "$pinned" ]; then
    echo "::error file=tools/openspec/package-lock.json::tools/openspec/package-lock.json pins no @fission-ai/openspec"
    failed=1
  elif ! version_at_least "$pinned" "$CLI_FLOOR"; then
    echo "::error file=tools/openspec/package-lock.json::the lockfile pins OpenSpec $pinned, older than $CLI_FLOOR, the first whose apply edits a repository that points at a store"
    failed=1
  fi

  if ((failed == 0)); then
    echo "openspec: this repository points at $STORE ($STORE_ID), keeps no planning of its own, and pins OpenSpec $pinned"
  fi
  return "$failed"
}

self_test() {
  local here work failures=0
  here="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
  work="$(mktemp -d)"

  fixture() { # name: a tree that passes, to be broken by the caller
    mkdir -p "$work/$1/openspec" "$work/$1/tools/openspec"
    printf '# A pointer, and a comment.\nstore: %s\n' "$STORE_ID" >"$work/$1/openspec/config.yaml"
    cp "$here/tools/openspec/package-lock.json" "$work/$1/tools/openspec/package-lock.json"
  }
  expect() { # name, pass|fail, text the output must hold
    local output status
    output="$(check "$work/$1" 2>&1)"
    status=$?
    if { [ "$2" = pass ] && ((status != 0)); } || { [ "$2" = fail ] && ((status == 0)); } ||
      ! printf '%s' "$output" | grep -qF -- "$3"; then
      echo "self-test: '$1' should $2 saying '$3', and it said:"
      printf '%s\n' "$output" | sed 's/^/    /'
      failures=$((failures + 1))
    fi
  }

  fixture pointer
  expect pointer pass "points at $STORE"

  fixture quoted
  printf "store: '%s'   # the store\n" "$STORE_ID" >"$work/quoted/openspec/config.yaml"
  expect quoted pass "points at $STORE"

  fixture specs
  mkdir -p "$work/specs/openspec/specs/payments"
  expect specs fail "openspec/specs exists"

  fixture changes
  mkdir -p "$work/changes/openspec/changes/archive"
  expect changes fail "openspec/changes exists"

  fixture context
  printf 'context: |\n  Local context.\n' >>"$work/context/openspec/config.yaml"
  expect context fail "context: |"

  fixture other-store
  printf 'store: another-store\n' >"$work/other-store/openspec/config.yaml"
  expect other-store fail "store: another-store"

  fixture two-stores
  printf 'store: %s\n' "$STORE_ID" >>"$work/two-stores/openspec/config.yaml"
  expect two-stores fail "nothing else"

  fixture no-config
  rm "$work/no-config/openspec/config.yaml"
  expect no-config fail "openspec/config.yaml is missing"

  fixture old-cli
  sed -i.bak '/"node_modules\/@fission-ai\/openspec"/,/}/s/"version": "[^"]*"/"version": "1.13.2"/' \
    "$work/old-cli/tools/openspec/package-lock.json"
  expect old-cli fail "pins OpenSpec 1.13.2"

  fixture newer-cli
  sed -i.bak '/"node_modules\/@fission-ai\/openspec"/,/}/s/"version": "[^"]*"/"version": "1.20.0"/' \
    "$work/newer-cli/tools/openspec/package-lock.json"
  expect newer-cli pass "pins OpenSpec 1.20.0"

  rm -rf "$work"
  if ((failures > 0)); then
    echo "::error::the self-test of scripts/check-openspec.sh failed $failures case(s)"
    return 1
  fi
  echo "self-test: scripts/check-openspec.sh passes and fails where it should (10 cases)"
}

if [ "${1:-}" = "--self-test" ]; then
  self_test
else
  check "${1:-$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)}"
fi
