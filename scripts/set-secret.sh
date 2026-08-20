#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
KEY="$(openssl rand -base64 32)"
dotnet user-secrets set "ShortCodes:Key" "$KEY" --project TinyLink.Api
[[ -f .env ]] || cp .env.example .env
if grep -q '^SHORTCODES__KEY=' .env; then
  sed -i.bak "s|^SHORTCODES__KEY=.*|SHORTCODES__KEY=$KEY|" .env && rm -f .env.bak
else
  printf 'SHORTCODES__KEY=%s\n' "$KEY" >> .env
fi
echo "Wrote new cipher key to user-secrets and .env"
