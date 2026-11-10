#!/usr/bin/env bash
# Run on the operator machine (not on the VM).
# Provisions the GCP VM, static IP, and firewall for the OTAI Desk demo.
set -euo pipefail

cd "$(dirname "$0")"
if [[ -f .env ]]; then
  set -a
  # shellcheck source=/dev/null
  source .env
  set +a
fi

: "${PROJECT_ID:?set PROJECT_ID in .env}"
: "${ZONE:?set ZONE}"
: "${REGION:?set REGION}"
: "${INSTANCE_NAME:?set INSTANCE_NAME}"
MACHINE_TYPE="${MACHINE_TYPE:-e2-standard-2}"
DISK_SIZE_GB="${DISK_SIZE_GB:-40}"
IMAGE_FAMILY="${IMAGE_FAMILY:-ubuntu-2404-lts-amd64}"
IMAGE_PROJECT="${IMAGE_PROJECT:-ubuntu-os-cloud}"
NETWORK_TAG="${NETWORK_TAG:-otai-desk-demo}"
ADMIN_SOURCE_RANGES="${ADMIN_SOURCE_RANGES:-0.0.0.0/0}"

gcloud config set project "$PROJECT_ID" >/dev/null

echo "==> Reserve static external IP (idempotent)"
if ! gcloud compute addresses describe "${INSTANCE_NAME}-ip" --region "$REGION" >/dev/null 2>&1; then
  gcloud compute addresses create "${INSTANCE_NAME}-ip" --region "$REGION"
fi
STATIC_IP="$(gcloud compute addresses describe "${INSTANCE_NAME}-ip" --region "$REGION" --format='value(address)')"
echo "    static IP: $STATIC_IP"

echo "==> Firewall rules (idempotent)"
gcloud compute firewall-rules describe "${NETWORK_TAG}-ssh" >/dev/null 2>&1 || \
  gcloud compute firewall-rules create "${NETWORK_TAG}-ssh" \
    --direction=INGRESS --action=ALLOW --rules=tcp:22 \
    --source-ranges="$ADMIN_SOURCE_RANGES" --target-tags="$NETWORK_TAG"

gcloud compute firewall-rules describe "${NETWORK_TAG}-http" >/dev/null 2>&1 || \
  gcloud compute firewall-rules create "${NETWORK_TAG}-http" \
    --direction=INGRESS --action=ALLOW --rules=tcp:80 \
    --source-ranges="$ADMIN_SOURCE_RANGES" --target-tags="$NETWORK_TAG"

gcloud compute firewall-rules describe "${NETWORK_TAG}-https" >/dev/null 2>&1 || \
  gcloud compute firewall-rules create "${NETWORK_TAG}-https" \
    --direction=INGRESS --action=ALLOW --rules=tcp:443 \
    --source-ranges="$ADMIN_SOURCE_RANGES" --target-tags="$NETWORK_TAG"

echo "==> Create instance (idempotent)"
if gcloud compute instances describe "$INSTANCE_NAME" --zone "$ZONE" >/dev/null 2>&1; then
  echo "    instance $INSTANCE_NAME already exists, skipping create"
else
  gcloud compute instances create "$INSTANCE_NAME" \
    --zone="$ZONE" \
    --machine-type="$MACHINE_TYPE" \
    --image-family="$IMAGE_FAMILY" --image-project="$IMAGE_PROJECT" \
    --boot-disk-size="${DISK_SIZE_GB}GB" --boot-disk-type=pd-balanced \
    --tags="$NETWORK_TAG" \
    --address="$STATIC_IP" \
    --metadata=enable-oslogin=TRUE
fi

echo
echo "Provisioned. Next:"
echo "  ./deploy-to-vm.sh --project $PROJECT_ID --zone $ZONE --vm $INSTANCE_NAME"
echo "Static IP: $STATIC_IP"
echo "Desk URL:  http://$STATIC_IP/"
