#!/usr/bin/env bash
# Gate de la fábrica: build + sintaxis + dist al día (+ compile check y smoke si hay entorno).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

step() { echo "==> $*"; }
skip() { echo "SKIP $*"; }
fail() { echo "FAIL: $*" >&2; exit 1; }

playwright_available() {
  command -v npm >/dev/null 2>&1 || return 1
  NODE_PATH="$(npm root -g 2>/dev/null)" node -e \
    "const {chromium}=require('playwright');process.exit(require('fs').existsSync(chromium.executablePath())?0:1)" \
    >/dev/null 2>&1
}

step "build"
command -v node >/dev/null 2>&1 || fail "build (node no está en PATH)"
node demo-web/build.mjs || fail "build"

step "node --check"
while IFS= read -r -d '' f; do
  node --check "$f" || fail "node --check $f"
done < <(find demo-web/src demo-web/tools -type f \( -name '*.js' -o -name '*.mjs' \) -print0; printf '%s\0' demo-web/build.mjs)

step "dist al día"
git diff --exit-code -- demo-web/dist/tag-school-demo.html >/dev/null \
  || fail "dist desfasado: ejecuta \`node demo-web/build.mjs\` y commitea"

step "compile check"
if command -v dotnet >/dev/null 2>&1; then
  python3 tools/unity-compile-check/compile_check.py || fail "compile check"
else
  skip "compile check: dotnet no está en PATH"
fi

step "smoke"
if playwright_available; then
  NODE_PATH="$(npm root -g)" node demo-web/tools/smoke.mjs || fail "smoke"
else
  skip "smoke: playwright/chromium no disponible"
fi

echo "OK"
