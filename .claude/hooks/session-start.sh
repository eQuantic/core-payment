#!/usr/bin/env bash
# SessionStart hook: prepares a session to work by the Workflow section of CLAUDE.md.
#
# In a cloud container (CLAUDE_CODE_REMOTE=true) it commits as the owner with signing off, installs
# the OpenSpec CLI the lockfile pins, clones and registers the central OpenSpec store beside the
# repository, installs the .NET SDK pinned below by version and SHA-256, and starts Docker. On a
# laptop it leaves the git identity, the signing, the SDKs, Docker and the store registry as their
# owner set them: it only installs the pinned OpenSpec CLI and puts it on the session's PATH, and
# checks that the store is registered.
#
# It never blocks the session. It prints one JSON object on stdout: what it prepared goes to the
# agent (additionalContext), and what it could not prepare also goes to the person (systemMessage).
# Everything else it runs writes to stderr.
#
# scripts/check-session-start.sh runs it the way a fresh container would. Tests point it at other
# sources with EQ_DOTNET_URL (the SDK archive) and EQ_SPECS_URL (the store to clone); the digest is
# checked whatever the URL, so neither can install an archive other than the pinned one.
set -uo pipefail

# The JSON goes to the saved stdout, everything else to stderr.
exec 3>&1 1>&2

OWNER_NAME='Edgar Mesquita'
OWNER_EMAIL='edgar@equantic.tech'

# .NET SDK 10.0.401, the latest 10.0 SDK when pinned, which global.json (10.0.100, latestFeature)
# accepts. Each digest is the SHA-256 of Microsoft's archive, taken from a download whose SHA-512
# matched https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json.
DOTNET_SDK_VERSION='10.0.401'
DOTNET_SDK_SHA256_LINUX_X64='137268c8ad939c064ff1ee2a6fdf0899d8725377114ea012fbd1ad5fa2550418'
DOTNET_SDK_SHA256_LINUX_ARM64='91d3d67f2ed065909bd2e1ba50a37192aff6df15178304939d5b55634d72da6a'

STORE_ID='equantic-specs'
STORE_URL="${EQ_SPECS_URL:-https://github.com/eQuantic/equantic-specs.git}"
STORE_SSH_URL='git@github.com:eQuantic/equantic-specs.git'

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd -P)"
remote=false
[ "${CLAUDE_CODE_REMOTE:-}" = "true" ] && remote=true
env_file="${CLAUDE_ENV_FILE:-}"

notes=()
warnings=()
note() { notes+=("$1"); echo "session-start: $1"; }
warn() { warnings+=("$1"); echo "session-start: warning: $1"; }

if [ -n "$env_file" ] && ! : >>"$env_file"; then
  warn "CLAUDE_ENV_FILE ($env_file) cannot be written, so nothing reaches the session's commands: no PATH, no identity"
  env_file=''
fi

# Exports for the session's later commands, through the file Claude Code sources before each one.
export_var() {
  [ -n "$env_file" ] || return 0
  printf 'export %s=%q\n' "$1" "$2" >>"$env_file"
}
export_path() {
  PATH="$1:$PATH"
  [ -n "$env_file" ] || return 0
  # $PATH stays literal, so each command prepends to the PATH it starts with.
  # shellcheck disable=SC2016
  printf 'export PATH=%q:"$PATH"\n' "$1" >>"$env_file"
}

json_escape() {
  local s=$1
  s=${s//\\/\\\\}
  s=${s//\"/\\\"}
  s=${s//$'\n'/\\n}
  s=${s//$'\r'/\\r}
  s=${s//$'\t'/\\t}
  printf '%s' "$s"
}

# The store goes beside the repository's main checkout, found through the common git directory, so
# a worktree under .claude/worktrees/ names the same folder ../equantic-specs names from the main one.
store_parent() {
  local common
  common="$(git -C "$repo" rev-parse --path-format=absolute --git-common-dir 2>/dev/null)" || common=''
  if [ -n "$common" ] && [ "$(basename "$common")" = ".git" ]; then
    dirname "$(dirname "$common")"
  else
    dirname "$repo"
  fi
}

prepare_identity() {
  local dir=$1
  git -C "$dir" config user.name "$OWNER_NAME" &&
    git -C "$dir" config user.email "$OWNER_EMAIL" &&
    git -C "$dir" config commit.gpgsign false &&
    git -C "$dir" config tag.gpgsign false || return 1
  # What git will actually use, whatever the environment injects above the repository's config.
  [ "$(git -C "$dir" config --get commit.gpgsign)" = "false" ] &&
    [ "$(git -C "$dir" config --get tag.gpgsign)" = "false" ]
}

prepare_git() {
  if ! prepare_identity "$repo"; then
    warn "git: could not commit as $OWNER_NAME <$OWNER_EMAIL> with signing off in $repo; run git config user.name, user.email, commit.gpgsign false and tag.gpgsign false there"
    return
  fi
  # An identity in the environment outranks every config file, the container's included.
  export_var GIT_AUTHOR_NAME "$OWNER_NAME"
  export_var GIT_AUTHOR_EMAIL "$OWNER_EMAIL"
  export_var GIT_COMMITTER_NAME "$OWNER_NAME"
  export_var GIT_COMMITTER_EMAIL "$OWNER_EMAIL"
  note "git: commits as $OWNER_NAME <$OWNER_EMAIL>, with commit and tag signing off in this repository"
}

node_is_supported() {
  command -v node >/dev/null 2>&1 && command -v npm >/dev/null 2>&1 &&
    node -e 'const [a, b] = process.versions.node.split(".").map(Number); process.exit(a > 20 || (a === 20 && b >= 19) ? 0 : 1)'
}

openspec_ready=false
prepare_openspec() {
  local tools="$repo/tools/openspec" bin="$repo/tools/openspec/node_modules/.bin" pinned installed
  if ! node_is_supported; then
    warn "OpenSpec CLI: it needs Node.js 20.19 or later and npm, and this machine has $(node --version 2>/dev/null || echo 'no Node.js'); /opsx and openspec will not work until it does"
    return
  fi
  pinned="$(node -p 'require(process.argv[1]).packages["node_modules/@fission-ai/openspec"].version' "$tools/package-lock.json" 2>/dev/null)" || pinned=''
  if [ -z "$pinned" ]; then
    warn "OpenSpec CLI: tools/openspec/package-lock.json pins no @fission-ai/openspec"
    return
  fi
  installed="$("$bin/openspec" --version 2>/dev/null)" || installed=''
  if [ "$installed" != "$pinned" ]; then
    if ! (cd "$tools" && OPENSPEC_TELEMETRY=0 npm ci --ignore-scripts --no-audit --no-fund); then
      warn "OpenSpec CLI: npm ci in tools/openspec failed, so /opsx and openspec will not work; run it again there"
      return
    fi
    installed="$("$bin/openspec" --version 2>/dev/null)" || installed=''
  fi
  if [ "$installed" != "$pinned" ]; then
    warn "OpenSpec CLI: tools/openspec answers '${installed:-nothing}' where the lockfile pins $pinned"
    return
  fi
  export OPENSPEC_TELEMETRY=0
  export_var OPENSPEC_TELEMETRY 0
  export_path "$bin"
  openspec_ready=true
  note "OpenSpec CLI $pinned on PATH, from tools/openspec, with telemetry off"
}

# A session commits proposals and archives in the store's checkout too.
prepare_store_identity() {
  if prepare_identity "$1"; then
    note "git: the store's checkout also commits as $OWNER_NAME <$OWNER_EMAIL>, with signing off"
  else
    warn "git: could not commit as $OWNER_NAME <$OWNER_EMAIL> with signing off in the store's checkout, $1; run git config user.name, user.email, commit.gpgsign false and tag.gpgsign false there"
  fi
}

registered_store_root() {
  openspec store list --json 2>/dev/null | node -e '
    let text = "";
    process.stdin.on("data", (chunk) => (text += chunk));
    process.stdin.on("end", () => {
      try {
        const store = (JSON.parse(text).stores || []).find((s) => s.id === process.argv[1]);
        if (store) process.stdout.write(store.root);
      } catch {}
    });' "$STORE_ID"
}

prepare_store() {
  local root dir
  $openspec_ready || return 0
  dir="$(store_parent)/$STORE_ID"
  root="$(registered_store_root)"
  if [ -n "$root" ] && [ -d "$root" ]; then
    $remote && prepare_store_identity "$root"
    note "OpenSpec store: $STORE_ID is registered at $root; pull it before starting (git -C $root pull --rebase)"
    return
  fi
  if ! $remote; then
    warn "OpenSpec store: $STORE_ID is not registered on this machine, so /opsx and openspec cannot reach the specs. Clone it beside this repository and register it: git clone $STORE_SSH_URL $dir && openspec store register $dir"
    return
  fi
  if [ ! -d "$dir/.git" ]; then
    if [ -e "$dir" ]; then
      warn "OpenSpec store: $dir exists and is not a git checkout, so the store was not cloned there"
      return
    fi
    if ! GIT_TERMINAL_PROMPT=0 git clone --quiet "$STORE_URL" "$dir"; then
      rm -rf "$dir"
      warn "OpenSpec store: could not clone $STORE_URL, as happens when this container has no access to the private repository; add eQuantic/equantic-specs to the session, then: git clone $STORE_URL $dir && openspec store register $dir"
      return
    fi
  fi
  if ! openspec store register "$dir"; then
    warn "OpenSpec store: openspec store register $dir failed"
    return
  fi
  prepare_store_identity "$dir"
  note "OpenSpec store: $STORE_ID cloned at $dir and registered"
}

sha256_of() {
  if command -v sha256sum >/dev/null 2>&1; then
    sha256sum "$1" | cut -d' ' -f1
  else
    shasum -a 256 "$1" | cut -d' ' -f1
  fi
}

prepare_dotnet() {
  local dir rid digest url tmp actual version
  if [ -z "${HOME:-}" ]; then
    warn ".NET SDK: HOME is not set, so there is nowhere to install dotnet"
    return
  fi
  dir="$HOME/.dotnet"
  case "$(uname -m)" in
    x86_64 | amd64) rid=linux-x64 digest=$DOTNET_SDK_SHA256_LINUX_X64 ;;
    aarch64 | arm64) rid=linux-arm64 digest=$DOTNET_SDK_SHA256_LINUX_ARM64 ;;
    *)
      warn ".NET SDK: no pinned archive for $(uname -m), so dotnet was not installed"
      return
      ;;
  esac
  if ! { [ -x "$dir/dotnet" ] && "$dir/dotnet" --list-sdks 2>/dev/null | grep -q "^$DOTNET_SDK_VERSION "; }; then
    url="${EQ_DOTNET_URL:-https://builds.dotnet.microsoft.com/dotnet/Sdk/$DOTNET_SDK_VERSION/dotnet-sdk-$DOTNET_SDK_VERSION-$rid.tar.gz}"
    tmp="$(mktemp -d)"
    if ! curl -fsSL --retry 3 --retry-delay 2 -o "$tmp/sdk.tar.gz" "$url"; then
      rm -rf "$tmp"
      warn ".NET SDK: the download of $DOTNET_SDK_VERSION failed ($url), so dotnet was not installed"
      return
    fi
    actual="$(sha256_of "$tmp/sdk.tar.gz")"
    if [ "$actual" != "$digest" ]; then
      rm -rf "$tmp"
      warn ".NET SDK: the $DOTNET_SDK_VERSION archive was refused, its SHA-256 is $actual and the pin is $digest; nothing was installed"
      return
    fi
    if ! { mkdir "$tmp/sdk" && tar -xzf "$tmp/sdk.tar.gz" -C "$tmp/sdk"; }; then
      rm -rf "$tmp"
      warn ".NET SDK: the $DOTNET_SDK_VERSION archive did not extract; nothing was installed"
      return
    fi
    if [ -e "$dir" ]; then
      cp -a "$tmp/sdk/." "$dir/"
    else
      mkdir -p "$(dirname "$dir")" && mv "$tmp/sdk" "$dir"
    fi || {
      rm -rf "$tmp"
      warn ".NET SDK: the $DOTNET_SDK_VERSION archive could not be put in $dir"
      return
    }
    rm -rf "$tmp"
  fi
  export DOTNET_ROOT="$dir" DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1
  export_var DOTNET_ROOT "$dir"
  export_var DOTNET_CLI_TELEMETRY_OPTOUT 1
  export_var DOTNET_NOLOGO 1
  export_path "$dir"
  # Answered in the repository, so global.json decides which SDK it is.
  version="$(cd "$repo" && "$dir/dotnet" --version 2>/dev/null)" || version=''
  if [ "$version" != "$DOTNET_SDK_VERSION" ]; then
    warn ".NET SDK: dotnet answers '${version:-nothing}' in this repository where $DOTNET_SDK_VERSION is pinned"
    return
  fi
  note ".NET SDK $DOTNET_SDK_VERSION on PATH, from $dir, verified by SHA-256"
}

prepare_docker() {
  local pid
  if ! command -v docker >/dev/null 2>&1; then
    warn "Docker: not in this container, so it could not be started; the build and the tests do not need it"
    return
  fi
  if docker info >/dev/null 2>&1; then
    note "Docker: running"
    return
  fi
  if ! command -v dockerd >/dev/null 2>&1; then
    warn "Docker: the client is here and no daemon is, so it could not be started; the build and the tests do not need it"
    return
  fi
  # Detached, and with no handle on the hook's stdout, which Claude Code reads to the end.
  setsid nohup dockerd >"${TMPDIR:-/tmp}/dockerd.log" 2>&1 </dev/null &
  pid=$!
  for _ in $(seq 1 30); do
    if docker info >/dev/null 2>&1; then
      note "Docker: started"
      return
    fi
    kill -0 "$pid" 2>/dev/null || break
    sleep 1
  done
  warn "Docker: the daemon did not start (${TMPDIR:-/tmp}/dockerd.log); the build and the tests do not need it"
}

if $remote; then
  prepare_git
  prepare_openspec
  prepare_store
  prepare_dotnet
  prepare_docker
else
  note "a local session: the git identity, the signing, the SDKs, Docker and the store registry are left as they are"
  prepare_openspec
  prepare_store
fi
[ -n "$env_file" ] || note "CLAUDE_ENV_FILE is not set, so nothing was exported to the session"

context="Session start for eQuantic/core-payment ($($remote && echo 'cloud container' || echo 'local machine')):"
for line in ${notes[@]+"${notes[@]}"} ${warnings[@]+"${warnings[@]}"}; do
  context+=$'\n'"- $line"
done
message=''
if ((${#warnings[@]})); then
  message='Session start could not prepare everything:'
  for line in "${warnings[@]}"; do
    message+=$'\n'"- $line"
  done
fi

{
  printf '{'
  [ -n "$message" ] && printf '"systemMessage":"%s",' "$(json_escape "$message")"
  printf '"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"%s"}}\n' "$(json_escape "$context")"
} >&3
exit 0
