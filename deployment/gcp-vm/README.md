# Deploy OTAI Desk demo on Google Compute Engine

Run the OTAI Desk API, Vue UI, Postgres, and Redis on a single Ubuntu VM with
Docker Compose. Caddy serves everything on port **80** (`/` → UI, `/api/*` → API).

## Prerequisites

- [Google Cloud SDK](https://cloud.google.com/sdk/docs/install) (`gcloud`) authenticated as a user with Compute Engine permissions (`gcloud auth login` as `tobias.bueck@openticketai.com`)
- GCP project with Compute Engine API enabled (default: `otai-studio-firebase`)

## 1. Provision the VM

```bash
cd otai-desk/deployment/gcp-vm
cp env.example .env
# edit .env if needed
bash 01-provision-vm.sh
```

Or on Windows after copying `.env`:

```powershell
cd otai-desk\deployment\gcp-vm
bash 01-provision-vm.sh
```

Creates:

- Static external IP `{INSTANCE_NAME}-ip`
- Firewall rules for SSH (22) and HTTP (80)
- Ubuntu 24.04 VM tagged `otai-desk-demo`

## 2. Deploy the stack

From the monorepo root context (includes `otai-vue-ui` for the UI build):

```powershell
cd otai-desk\deployment\gcp-vm
.\Deploy-ToGceVm.ps1 -ProjectId otai-studio-firebase -Zone europe-west3-a -VmName otai-desk-demo
```

Linux / Git Bash:

```bash
./deploy-to-vm.sh --project otai-studio-firebase --zone europe-west3-a --vm otai-desk-demo
```

First deploy builds images on the VM (several minutes). Subsequent deploys reuse layers.

## 3. Verify

```bash
curl http://STATIC_IP/health
# -> {"status":"ok"}
```

Open `http://STATIC_IP/` in a browser for the Desk UI. API docs: `http://STATIC_IP/docs`.

OpenID Connect and the directory come up with the same stack. Keycloak is at `http://STATIC_IP/auth/` (admin user `admin`, password from `KEYCLOAK_ADMIN_PASSWORD`). The realm is `otai`, the client is `otai-desk`, and the issuer is `http://STATIC_IP/auth/realms/otai`. Users `admin`, `agent`, and `employee` live in OpenLDAP (`dc=openticketai,dc=test`) with password `demo-password` and groups `helpdesk-admins`, `helpdesk-agents`, and `helpdesk-employees`. LDAP is only on the Compose network: `ldap://openldap:389`.

On first start the API seeds demo data automatically (`OTAI_DESK_SEED_DEMO=true`): sample users, queues, tickets, a conversation, knowledge docs, and canned responses. To run the seed manually:

```bash
sudo docker compose exec desk python -m otai_desk.demo_seed
```

To re-seed from scratch, reset the database volume first:

```bash
sudo docker compose down -v
sudo docker compose up -d --build
```

## Updating

Re-run `Deploy-ToGceVm.ps1` or `deploy-to-vm.sh`. Demo secrets in `~/otai-desk-demo/otai-desk/deployment/demo/.env` on the VM are
preserved after the first bootstrap (see `../demo/env.example` for keys).

## SSH

```powershell
gcloud compute ssh --project otai-studio-firebase --zone europe-west3-a otai-desk-demo
```

On the VM:

```bash
cd ~/otai-desk-demo/otai-desk/deployment/demo
sudo docker compose ps
sudo docker compose logs -f desk
```

## Teardown

```bash
gcloud compute instances delete otai-desk-demo --zone europe-west3-a --quiet
gcloud compute addresses delete otai-desk-demo-ip --region europe-west3 --quiet
```
