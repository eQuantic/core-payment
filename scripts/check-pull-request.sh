#!/usr/bin/env bash
# A pull request follows the Workflow section of CLAUDE.md where a machine can tell:
#
#   - its branch is <type>/<slug>, with the type one of feat, fix, chore, refactor, docs, test, ci,
#     perf or build (Dependabot's own dependabot/... branches aside), never the claude/... branch a
#     session suggests;
#   - its title, which the squash merge makes the commit message semantic-release reads, is
#     `emoji type: description`, with the emoji of its type;
#   - its body carries no attribution to Claude: no co-authorship trailer, no session link, no
#     generated-with footer.
#
# The pull request comes in through the environment, never through the command line, so nothing in
# it is ever run: HEAD_REF (the branch), PR_TITLE and PR_BODY.
#
#   HEAD_REF=… PR_TITLE=… PR_BODY=… scripts/check-pull-request.sh
#   scripts/check-pull-request.sh --self-test    proves the check both ways
set -uo pipefail

TYPES='feat|fix|chore|refactor|docs|test|ci|perf|build'
# The emoji of each type, as the commits in this repository write them; ♻️ and ⚡ may come with or
# without the variation selector.
TITLE_PATTERN='^(✨ feat|🐛 fix|📝 docs|🔧 chore|♻️ refactor|♻ refactor|✅ test|👷 ci|⚡️ perf|⚡ perf|📦 build)(\([^)]+\))?!?: [^[:space:]].*$'
# What Claude Code and its tools append when nobody turns them off.
ATTRIBUTION_PATTERN='(generated with \[?claude code|co-authored-by:.*(claude|anthropic)|claude-session:|claude\.ai/code/session_)'

check() {
  local head_ref=$1 title=$2 body=$3 failed=0

  if [ -z "$head_ref" ]; then
    echo "::error::the runner gave no pull request branch (HEAD_REF is empty), so the conventions cannot be checked"
    return 1
  fi
  if [[ "$head_ref" == dependabot/* ]]; then
    :
  elif ! [[ "$head_ref" =~ ^($TYPES)/[a-z0-9][a-z0-9._-]*$ ]]; then
    echo "::error::the branch '$head_ref' is not <type>/<slug>, with the type one of ${TYPES//|/, } and a lowercase slug; a session's claude/... branch is never used"
    failed=1
  fi

  if ! printf '%s' "$title" | grep -qE "$TITLE_PATTERN"; then
    echo "::error::the title '$title' is not 'emoji type: description' (✨ feat, 🐛 fix, 📝 docs, 🔧 chore, ♻️ refactor, ✅ test, 👷 ci, ⚡ perf, 📦 build); the squash merge makes it the commit message semantic-release reads"
    failed=1
  fi

  if printf '%s' "$body" | grep -qiE "$ATTRIBUTION_PATTERN"; then
    echo "::error::the body carries an attribution to Claude, which this repository never does; remove it:"
    printf '%s\n' "$body" | grep -iE "$ATTRIBUTION_PATTERN" | sed 's/^/    /'
    failed=1
  fi

  if ((failed == 0)); then
    echo "pull request: the branch, the title and the body follow the Workflow section of CLAUDE.md"
  fi
  return "$failed"
}

self_test() {
  local failures=0 cases=0
  expect() { # pass|fail, text the output must hold, head_ref, title, body
    local output status
    cases=$((cases + 1))
    output="$(check "$3" "$4" "$5" 2>&1)"
    status=$?
    if { [ "$1" = pass ] && ((status != 0)); } || { [ "$1" = fail ] && ((status == 0)); } ||
      ! printf '%s' "$output" | grep -qF -- "$2"; then
      echo "self-test: '$3' / '$4' should $1 saying '$2', and it said:"
      printf '%s\n' "$output" | sed 's/^/    /'
      failures=$((failures + 1))
    fi
  }
  local ok='follow the Workflow section'

  expect pass "$ok" 'feat/stripe-saved-cards' '✨ feat: saved cards on Stripe' 'Closes #12.'
  expect pass "$ok" 'chore/working-agreement' '🔧 chore: the working agreement lives in the repository' ''
  expect pass "$ok" 'refactor/one-mapper' '♻️ refactor: one mapper per wire model' ''
  expect pass "$ok" 'perf/fewer-allocations' '⚡ perf: fewer allocations per request' ''
  expect pass "$ok" 'fix/pix-expiry' '🐛 fix(stripe)!: a Pix expires when it says' ''
  expect pass "$ok" 'dependabot/nuget/microsoft-4887cec49e' '🔧 chore: Bump Microsoft.Extensions.Http' ''
  expect pass "$ok" 'docs/no-footer' '📝 docs: the template' 'It says: no generated-with footer, no session link.'
  expect fail "is not <type>/<slug>" 'claude/setup-working-agreement-x1Y2z' '🔧 chore: rules' ''
  expect fail "is not <type>/<slug>" 'feature/saved-cards' '✨ feat: saved cards' ''
  expect fail "is not <type>/<slug>" 'feat/Saved_Cards' '✨ feat: saved cards' ''
  expect fail "HEAD_REF is empty" '' '✨ feat: saved cards' ''
  expect fail "is not 'emoji type: description'" 'feat/saved-cards' 'feat: saved cards' ''
  expect fail "is not 'emoji type: description'" 'feat/saved-cards' '✨ feature: saved cards' ''
  expect fail "is not 'emoji type: description'" 'feat/saved-cards' '🐛 feat: saved cards' ''
  expect fail "is not 'emoji type: description'" 'feat/saved-cards' '✨ feat:saved cards' ''
  expect fail "attribution to Claude" 'feat/saved-cards' '✨ feat: saved cards' $'Closes #12.\n\n🤖 Generated with [Claude Code](https://claude.com/claude-code)'
  expect fail "attribution to Claude" 'feat/saved-cards' '✨ feat: saved cards' $'Closes #12.\n\nCo-Authored-By: Claude <noreply@anthropic.com>'
  expect fail "attribution to Claude" 'feat/saved-cards' '✨ feat: saved cards' $'Closes #12.\n\nhttps://claude.ai/code/session_01AbCdEf'

  if ((failures > 0)); then
    echo "::error::the self-test of scripts/check-pull-request.sh failed $failures of $cases case(s)"
    return 1
  fi
  echo "self-test: scripts/check-pull-request.sh passes and fails where it should ($cases cases)"
}

if [ "${1:-}" = "--self-test" ]; then
  self_test
else
  check "${HEAD_REF:-}" "${PR_TITLE:-}" "${PR_BODY:-}"
fi
