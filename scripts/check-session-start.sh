#!/usr/bin/env bash
# Runs .claude/hooks/session-start.sh the way a fresh cloud container would, and the way a laptop
# would, and checks each of its promises. The hook's cloud path is one no laptop takes, so nothing
# else would notice the day it broke.
#
#   cloud    a commit fails before the hook, as the container's global git config requires a
#            signature with a key that is not there, and after it a commit is the owner's, unsigned;
#            dotnet answers the pinned SDK in the repository, from the hook's own install; openspec
#            answers the version the lockfile pins, with telemetry off; the store is cloned beside
#            the repository and registered; and the person is told nothing, as nothing fell short.
#   controls an SDK archive whose SHA-256 is not the pinned one is refused, nothing is installed,
#            and the person is told; a store that cannot be cloned leaves nothing where it would have
#            gone, and the person is told the command that registers it.
#   laptop   the identity and the signing are left alone, nothing is cloned or installed but the
#            pinned OpenSpec CLI, and the person is warned with the commands that set the store up,
#            until the store is registered.
#
# Each run starts from an empty environment, with a home of its own and a hostile global git config.
# The store is a local fixture (EQ_SPECS_URL), since CI cannot read the private one; the SDK is the
# real download, once.
#
#   scripts/check-session-start.sh
set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd -P)"
hook="$root/.claude/hooks/session-start.sh"
work="$(mktemp -d)"
work="$(cd "$work" && pwd -P)"
trap 'rm -rf "$work"' EXIT

owner='Edgar Mesquita <edgar@equantic.tech>'
pinned_sdk="$(sed -n "s/^DOTNET_SDK_VERSION='\(.*\)'$/\1/p" "$hook")"
pinned_cli="$(node -p 'require(process.argv[1]).packages["node_modules/@fission-ai/openspec"].version' "$root/tools/openspec/package-lock.json")"

failures=0
ok() { echo "ok: $1"; }
fail() {
  echo "::error::session-start: $1"
  failures=$((failures + 1))
}
expect_equal() { # what, actual, expected
  if [ "$2" = "$3" ]; then ok "$1"; else fail "$1: got '$2', expected '$3'"; fi
}
expect_contains() { # what, text, needle
  if printf '%s' "$2" | grep -qF -- "$3"; then ok "$1"; else fail "$1: '$3' not found in: $2"; fi
}

# Only what the network needs survives from the runner's environment.
base_env=(PATH="$PATH" LANG=C.UTF-8)
for name in HTTPS_PROXY https_proxy HTTP_PROXY http_proxy NO_PROXY no_proxy SSL_CERT_FILE SSL_CERT_DIR \
  NODE_EXTRA_CA_CERTS CURL_CA_BUNDLE REQUESTS_CA_BUNDLE GIT_SSL_CAINFO; do
  [ -n "${!name:-}" ] && base_env+=("$name=${!name}")
done
while IFS='=' read -r name _; do
  base_env+=("$name=${!name}")
done < <(env | grep -iE '^npm_config_[a-z_]+=' || true)

# A home whose global git config names someone else and signs with a key that does not exist.
hostile_home() {
  mkdir -p "$1"
  cat >"$1/.gitconfig" <<EOF
[user]
	name = Claude
	email = noreply@anthropic.com
	signingkey = $1/missing-signing-key.pub
[gpg]
	format = ssh
[commit]
	gpgsign = true
[tag]
	gpgsign = true
EOF
}

# The working tree as a repository of its own: tracked files and new ones, never ignored ones.
copy_repo() {
  mkdir -p "$1"
  (cd "$root" && git ls-files -z --cached --others --exclude-standard | tar --null -T - -cf -) | tar -xf - -C "$1"
  git -C "$1" init -q -b main
  git -C "$1" add -A
}

# A store with the shape `openspec store register` accepts, as a bare repository to clone.
store_src="$work/store-src"
mkdir -p "$store_src/.openspec-store" "$store_src/openspec/specs" "$store_src/openspec/changes/archive"
printf 'version: 1\nid: equantic-specs\nremote: git@github.com:eQuantic/equantic-specs.git\n' >"$store_src/.openspec-store/store.yaml"
printf 'schema: spec-driven\n' >"$store_src/openspec/config.yaml"
touch "$store_src/openspec/specs/.gitkeep" "$store_src/openspec/changes/archive/.gitkeep"
git -C "$store_src" init -q -b main
git -C "$store_src" add -A
git -C "$store_src" -c user.name=fixture -c user.email=fixture@example.invalid -c commit.gpgsign=false \
  commit -q -m 'the fixture store'
git clone -q --bare "$store_src" "$work/store.git"

run_hook() { # repository, home, cloud|laptop, env file, extra variables...
  local repo=$1 home=$2 mode=$3 env_file=$4
  shift 4
  local -a vars=("${base_env[@]}" HOME="$home" TMPDIR="$work/tmp" CLAUDE_ENV_FILE="$env_file"
    CLAUDE_PROJECT_DIR="$repo" EQ_SPECS_URL="file://$work/store.git")
  [ "$mode" = cloud ] && vars+=(CLAUDE_CODE_REMOTE=true)
  mkdir -p "$work/tmp"
  : >"$env_file"
  # The copy's own hook, which prepares the copy it sits in.
  env -i "${vars[@]}" "$@" bash "$repo/.claude/hooks/session-start.sh" 2>"$env_file.log"
}

in_session() { # home, env file, command...: a later command of the session the hook prepared
  local home=$1 env_file=$2
  shift 2
  # Expanded by the inner shell, after it has sourced what the hook exported.
  # shellcheck disable=SC2016
  env -i "${base_env[@]}" HOME="$home" bash -c 'source "$0"; eval "$1"' "$env_file" "$*"
}

field() { # JSON, field: systemMessage or additionalContext
  printf '%s' "$1" | node -e '
    let text = "";
    process.stdin.on("data", (chunk) => (text += chunk));
    process.stdin.on("end", () => {
      const out = JSON.parse(text);
      if (out.hookSpecificOutput?.hookEventName !== "SessionStart") process.exit(3);
      const value = process.argv[1] === "systemMessage" ? out.systemMessage : out.hookSpecificOutput.additionalContext;
      process.stdout.write(value ?? "");
    });' "$2"
}

echo "== a fresh cloud container"
home="$work/home"
hostile_home "$home"
copy_repo "$work/a/repo"
if env -i "${base_env[@]}" HOME="$home" git -C "$work/a/repo" commit -q -m 'before the hook' >/dev/null 2>&1; then
  fail "the control did not bite: a commit before the hook succeeded under the hostile global config"
else
  ok "before the hook, a commit fails under the container's identity and signing"
fi
out="$(run_hook "$work/a/repo" "$home" cloud "$work/a.env")"
status=$?
expect_equal "the hook exits 0" "$status" 0
if ! message="$(field "$out" systemMessage)"; then
  fail "the hook's stdout is not the SessionStart JSON: $out"
  sed 's/^/    /' "$work/a.env.log"
else
  expect_equal "nothing is said to the person" "$message" ""
  [ -z "$message" ] || sed 's/^/    /' "$work/a.env.log"
fi
if in_session "$home" "$work/a.env" "git -C '$work/a/repo' commit -q -m 'after the hook'" >/dev/null 2>&1; then
  expect_equal "after the hook, a commit is the owner's and unsigned" \
    "$(git -C "$work/a/repo" log -1 --format='%an <%ae>|%cn <%ce>|%G?')" "$owner|$owner|N"
else
  fail "after the hook, a commit still fails"
fi
expect_equal "dotnet is the hook's own install" "$(in_session "$home" "$work/a.env" 'command -v dotnet')" "$home/.dotnet/dotnet"
expect_equal "dotnet answers the pinned SDK in the repository" \
  "$(in_session "$home" "$work/a.env" "cd '$work/a/repo' && dotnet --version")" "$pinned_sdk"
expect_equal "openspec answers the pinned CLI" "$(in_session "$home" "$work/a.env" 'openspec --version')" "$pinned_cli"
# shellcheck disable=SC2016 # expanded in the session, not here
expect_equal "OpenSpec telemetry is off" "$(in_session "$home" "$work/a.env" 'printf %s "$OPENSPEC_TELEMETRY"')" 0
if [ -d "$work/a/equantic-specs/.git" ]; then ok "the store is cloned beside the repository"; else fail "the store was not cloned beside the repository"; fi
expect_contains "the store is registered there" \
  "$(in_session "$home" "$work/a.env" 'openspec store list --json')" "\"root\": \"$work/a/equantic-specs\""

echo "== an SDK archive that is not the pinned one"
tar -czf "$work/not-the-sdk.tar.gz" -C "$store_src" .openspec-store
copy_repo "$work/b/repo"
hostile_home "$work/home-b"
rm -rf "$work/tmp"
out="$(run_hook "$work/b/repo" "$work/home-b" cloud "$work/b.env" EQ_DOTNET_URL="file://$work/not-the-sdk.tar.gz")"
expect_contains "the person is told the .NET SDK was refused" "$(field "$out" systemMessage)" ".NET SDK: the $pinned_sdk archive was refused"
if [ -e "$work/home-b/.dotnet" ]; then fail "a refused SDK left $work/home-b/.dotnet behind"; else ok "a refused SDK installs nothing"; fi
expect_equal "and leaves no download behind" "$(find "$work/tmp" -name 'sdk*' | head -n 1)" ""

echo "== a store the container cannot reach"
copy_repo "$work/c/repo"
out="$(run_hook "$work/c/repo" "$home" cloud "$work/c.env" XDG_DATA_HOME="$work/registry-c" EQ_SPECS_URL="file://$work/no-such-store.git")"
message="$(field "$out" systemMessage)"
expect_contains "the person is told the store could not be cloned" "$message" "OpenSpec store: could not clone"
expect_contains "with the command that registers it" "$message" "openspec store register $work/c/equantic-specs"
if [ -e "$work/c/equantic-specs" ]; then fail "a failed clone left $work/c/equantic-specs behind"; else ok "a failed clone leaves nothing behind"; fi

echo "== a laptop"
copy_repo "$work/d/repo"
hostile_home "$work/home-d"
out="$(run_hook "$work/d/repo" "$work/home-d" laptop "$work/d.env" XDG_DATA_HOME="$work/registry-d")"
message="$(field "$out" systemMessage)"
expect_contains "the person is warned the store is not registered" "$message" "equantic-specs is not registered on this machine"
expect_contains "with the commands that set it up" "$message" "git clone git@github.com:eQuantic/equantic-specs.git $work/d/equantic-specs && openspec store register $work/d/equantic-specs"
expect_contains "the agent is told what was left alone" "$(field "$out" additionalContext)" "are left as they are"
expect_equal "the identity is left alone" "$(git -C "$work/d/repo" config --local --get user.name)" ""
expect_equal "the signing is left alone" "$(git -C "$work/d/repo" config --local --get commit.gpgsign)" ""
if grep -q 'GIT_' "$work/d.env"; then fail "a laptop's session was given a git identity"; else ok "a laptop's session is given no git identity"; fi
if [ -e "$work/home-d/.dotnet" ]; then fail "a laptop was given a .NET SDK"; else ok "a laptop is given no .NET SDK"; fi
if [ -e "$work/d/equantic-specs" ]; then fail "a laptop had the store cloned"; else ok "a laptop has nothing cloned"; fi
expect_equal "the pinned CLI is on the laptop session's PATH" "$(in_session "$work/home-d" "$work/d.env" 'openspec --version')" "$pinned_cli"
git clone -q "$work/store.git" "$work/d/equantic-specs"
env -i "${base_env[@]}" HOME="$work/home-d" XDG_DATA_HOME="$work/registry-d" OPENSPEC_TELEMETRY=0 \
  "$work/d/repo/tools/openspec/node_modules/.bin/openspec" store register "$work/d/equantic-specs" >/dev/null
out="$(run_hook "$work/d/repo" "$work/home-d" laptop "$work/d.env" XDG_DATA_HOME="$work/registry-d")"
expect_equal "once the store is registered, the laptop gets no warning" "$(field "$out" systemMessage)" ""

if ((failures > 0)); then
  echo "::error::scripts/check-session-start.sh: $failures promise(s) of the session-start hook broken"
  exit 1
fi
echo "session-start: every promise of the hook holds"
