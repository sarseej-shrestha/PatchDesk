#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
if [[ -x "$root/.tools/dotnet/dotnet" ]]; then
  export DOTNET_ROOT="$root/.tools/dotnet"
  export DOTNET_CLI_HOME="$root/.tools/home"
  export NUGET_PACKAGES="$root/.tools/nuget"
  export PATH="$DOTNET_ROOT:$PATH"
fi
exec dotnet "$@"
