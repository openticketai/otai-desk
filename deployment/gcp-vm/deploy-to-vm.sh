#!/usr/bin/env bash
# Sync otai-desk demo bundle to a GCE VM and run docker compose.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../../.." && pwd)"

PROJECT=""
ZONE=""
VM=""
REMOTE_USER="${USER:-$(whoami)}"
REMOTE_DIR="otai-desk-demo"

usage() {
  echo "Usage: $0 --project ID --zone ZONE --vm NAME [--remote-user USER]" >&2
  exit 1
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -p|--project) PROJECT="$2"; shift 2 ;;
    -z|--zone) ZONE="$2"; shift 2 ;;
    -v|--vm) VM="$2"; shift 2 ;;
    -u|--remote-user) REMOTE_USER="$2"; shift 2 ;;
    -h|--help) usage ;;
    *) echo "Unknown arg: $1" >&2; usage ;;
  esac
done

if [[ -z "$PROJECT" || -z "$ZONE" || -z "$VM" ]]; then
  usage
fi

TMP_TAR="${TMPDIR:-/tmp}/otai-desk-demo-$$-${RANDOM}.tar.gz"
cleanup() { rm -f "${TMP_TAR}"; }
trap cleanup EXIT

echo "Creating bundle from ${REPO_ROOT}..."
tar -czf "${TMP_TAR}" \
  --exclude='.git' \
  --exclude='node_modules' \
  --exclude='__pycache__' \
  --exclude='*.pyc' \
  --exclude='.venv' \
  --exclude='.pytest_cache' \
  --exclude='.ruff_cache' \
  -C "${REPO_ROOT}" \
  otai-desk \
  otai-website/packages/otai-vue-ui

gcloud compute scp "${TMP_TAR}" \
  "${REMOTE_USER}@${VM}:~/otai-desk-demo-bundle.tar.gz" \
  --project="${PROJECT}" \
  --zone="${ZONE}"

gcloud compute scp "${SCRIPT_DIR}/02-bootstrap-vm.sh" \
  "${REMOTE_USER}@${VM}:~/02-bootstrap-vm.sh" \
  --project="${PROJECT}" \
  --zone="${ZONE}"

gcloud compute ssh "${REMOTE_USER}@${VM}" \
  --project="${PROJECT}" \
  --zone="${ZONE}" \
  --command="rm -rf ~/${REMOTE_DIR} && mkdir -p ~/${REMOTE_DIR} && tar -xzf ~/otai-desk-demo-bundle.tar.gz -C ~/${REMOTE_DIR} && rm -f ~/otai-desk-demo-bundle.tar.gz && bash ~/02-bootstrap-vm.sh"

echo "Done. Open: http://$(gcloud compute instances describe ${VM} --project=${PROJECT} --zone=${ZONE} --format='value(networkInterfaces[0].accessConfigs[0].natIP)')/"
