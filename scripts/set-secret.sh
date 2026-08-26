#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
KEY="$(openssl rand -base64 32)"
dotnet user-secrets set "ShortCodes:Key" "$KEY" --project TinyLink.Api
[[ -f .env ]] || cp .env.example .env
grep -vi '^ShortCodes__Key=' .env > .env.tmp || true
mv .env.tmp .env
printf 'ShortCodes__Key=%s\n' "$KEY" >> .env

