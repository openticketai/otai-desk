#!/usr/bin/env bash
# Runs ON the VM after the bundle is uploaded to ~/otai-desk-demo.
set -euo pipefail

DEMO_DIR="${HOME}/otai-desk-demo"
COMPOSE_DIR="${DEMO_DIR}/otai-desk/deployment/demo"
ENV_FILE="${COMPOSE_DIR}/.env"

cd "$DEMO_DIR"

echo "==> Install Docker Engine + Compose plugin (idempotent)"
if ! command -v docker >/dev/null 2>&1; then
  sudo DEBIAN_FRONTEND=noninteractive apt-get update -y
  sudo DEBIAN_FRONTEND=noninteractive apt-get install -y ca-certificates curl gnupg lsb-release
  sudo install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
    | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  sudo chmod a+r /etc/apt/keyrings/docker.gpg
  echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" \
    | sudo tee /etc/apt/sources.list.d/docker.list >/dev/null
  sudo apt-get update -y
  sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
  sudo usermod -aG docker "$USER" || true
fi

if [[ ! -f "$ENV_FILE" ]]; then
  echo "==> Write demo .env"
  SECRET="$(openssl rand -hex 32)"
  DB_PW="$(openssl rand -hex 16)"
  cat >"$ENV_FILE" <<EOF
POSTGRES_PASSWORD=${DB_PW}
OTAI_DESK_SECRET_KEY=${SECRET}
OTAI_DESK_CORS_ORIGINS=*
OTAI_DESK_SEED_DEMO=true
NUXT_PUBLIC_API_BASE=
EOF
fi

echo "==> Build and start OTAI Desk demo stack"
cd "$COMPOSE_DIR"
sudo docker compose pull
sudo docker compose up -d --build
sudo docker compose ps

echo
echo "Health check:"
curl -fsS http://127.0.0.1/health || true
echo
