#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "$0")" && pwd)"
ENV_FILE="${ROOT}/.env"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "==> Generate .env with random secrets"
  SECRET="$(openssl rand -hex 32)"
  DB_PW="$(openssl rand -hex 16)"
  cat >"$ENV_FILE" <<EOF
POSTGRES_PASSWORD=${DB_PW}
OTAI_DESK_SECRET_KEY=${SECRET}
OTAI_DESK_CORS_ORIGINS=http://localhost
EOF
  chmod 600 "$ENV_FILE" 2>/dev/null || true
else
  echo "==> .env already exists (skipping generation)"
fi

cd "$ROOT"
echo "==> Build and start OTAI Desk"
docker compose up -d --build
docker compose ps

echo
echo "Open http://localhost"
