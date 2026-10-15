#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$root"
if [[ -d .tools/node/bin ]]; then export PATH="$root/.tools/node/bin:$PATH"; fi
if [[ -d .tools/playwright ]]; then export PLAYWRIGHT_BROWSERS_PATH="$root/.tools/playwright"; fi
exec npm --prefix frontend run test:e2e -- "$@"
