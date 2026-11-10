---
id: LD-082
status: "accepted"
date: 2026-09-20
supersedes: [LD-081]
superseded_by: []
legacy_status: Locked
categories: [product-ux, persistence]
---

# OTAI Desk one portal RBAC plus ticket-system sync replica

## Context and Problem Statement

LD-081 locked OTAI Desk Phase 1 as a **customer-only** portal that must **not**
store ticket rows (sessions/org/audit only) and postponed any agent UI to Phase 2. That split two products (customer portal vs agent portal) and forced a
passthrough architecture.

Buyers still need **zero migration**: keep OTOBO / Znuny / Zammad / KIX as the
system of record. Operators also need **one** Desk UI where a customer and a
first-level agent use the same app with different rights. Desk already has a
local ticket schema; throwing it away just to re-add it in Phase 3 is waste.
A local copy is useful for portal latency, search, and the mini agent queue —
as long as it stays a **replica** that syncs.

Studio Community Edition (chatbot-only OSS on-ramp) from LD-081 remains needed.

## Decision Drivers

- One SPA, not two deployables or two “portals”.
- Rights (RBAC) decide visible tickets, fields, and actions — not a product
  fork.
- Existing ticket system stays system of record until Phase 3 is explicitly
  opened.
- Local Postgres may hold tickets, articles, users, and FAQ copies.
- `otai-ts-connector` is the sync I/O surface (LD-034, LD-049).
- SPA never holds agent or admin ticket-system tokens (LD-070 session rule).
- Community Studio + three chatbot tools stay the AI on-ramp (LD-047, LD-062).

## Considered Options

- Keep LD-081: customer-only passthrough, no ticket rows, agent UI later.
- Two portals (customer SPA + agent SPA) over the same API.
- Desk as system of record in Phase 1 (rip-and-replace).
- **One portal + RBAC + local replica synced to the existing ticket system.**

## Decision Outcome

Chosen option: **one OTAI Desk portal; users differ by rights; Phase 1 is the
customer + mini-agent frontend over the existing ticket system, with a local
ticket replica that syncs.**

LD-081 is superseded for **portal split, Phase 1 agent scope, and “no ticket
rows”**. Community Edition, LGPL Desk, connector session rules, and
`CUSTOMER_INTERFACE` stay in force (restated below).

### Binding rules — one portal and RBAC

1. **One UI:** `otai-desk/line/frontend` is the only portal (LD-092). Do not ship a separate
   customer-portal or agent-portal app, and do not ship a second Nuxt app. Menus, queues, and fields filter by
   permission.
2. **Roles (Phase 1):** at least `customer` and `agent`. A user may hold one
   role per org. Permissions gate list scope (own tickets vs queue), article
   visibility, status change, and assignment **only if** the ticket system
   exposes that write.
3. **Mini agent (Phase 1):** focused queue + ticket zoom + reply + status
   change that syncs. No SLA admin, no plugin marketplace, no canned
   responses, no full workforce management.
4. **Auth:** Desk API owns the session (HTTP-only cookie). Role comes from
   Desk user + mapped ticket-system identity. Never put TS agent tokens in
   the browser.

### Binding rules — replica and sync

1. **System of record:** the configured ticket core (LD-092). `desk` means local
   Postgres. `otobo`, `znuny`, `zammad`, or `kix` means that system, with a Desk replica.
2. **Local DB:** Desk **does** persist ticket, article, user-map, and FAQ
   replica rows (plus sessions, org branding, audit). This is not Phase 3
   native SoR.
3. **Write path:** user writes hit Desk, then **push** through
   `otai-ts-connector`. Persist the replica only after the connector accepts
   the write, or persist pending and reconcile — never silently diverge.
4. **Read path:** portal may read the replica. A sync job (pull) refreshes
   from the ticket system. On conflict, **ticket system wins**.
5. **Identity map:** every replica ticket stores the remote id + provider.
   Do not invent a second ticket number as the customer-facing id unless it
   is the remote number.
6. **Fail loud:** external mode fails the write when that ticket system is down.
   Do not pretend Desk is the source of record for an external provider. `desk`
   mode is the local source of record (LD-092).

### Binding rules — retained from LD-081

1. **License:** Desk Phase 1 is LGPL-2.1-or-later. Connector stays Apache-2.0.
2. **Deployables:** `otai-desk/desk` API + Vue UI (LD-092). Do not start
   `apps/customer-portal-*` or a second portal app.
3. **`CUSTOMER_INTERFACE`:** `modern` | `native` (fail-loud). `modern` is
   Desk; `native` is the vendor customer UI. Chat may embed on either.
4. **Studio Community Edition:** chatbot-only OSS control plane; Ollama or
   OpenRouter BYOK; tools `search_faq`, `create_ticket`, `web_fetch` only.
   JWT Full On-Prem unlocks commercial Studio + Runtime (LD-047). Config pull
   LD-062.
5. **Phase 3:** replaced by LD-085 — Desk never becomes SoR. Optional later
   OTAI ticket backend is a fifth connector target.

## Consequences

- Good: one product surface; rights scale from customer to mini agent.
- Good: local tickets match existing Desk schema work and make the UI fast.
- Good: sales pitch stays “keep your helpdesk”.
- Trade-off: sync, identity mapping, and conflict rules are real work.
- Trade-off: replica is not a second source of truth — do not build offline
  edit as a feature.
- Agents: the phase-1 drawing was removed. The current picture is `otai-desk-architecture` (LD-097). The write-up is `otai-desk/docs/phase-1-customer-portal-plan.md`.

## Links

- Stable id: `LD-082`
- Index: `otai-desk/docs/adr/`
- Supersedes: LD-081 (`otai/docs/adr/0081-…`)
- Related: [LD-029](./0029-proprietary-jwt-license-runtime-oss.md),
  [LD-034](./0034-shared-ticket-sdk.md),
  [LD-047](./0047-unified-license-type-entitlements.md),
  [LD-049](./0049-agent-tools-in-process-mcp-adapter.md),
  [LD-062](./0062-runtime-chatbot-pull-config-from-studio.md),
  [LD-070](./0070-modern-customer-portal-spa.md),
  [LD-085](./0085-otai-desk-owns-rbac-portal-mode-own-ticket-backend.md)
- Plan: `otai-desk/docs/phase-1-customer-portal-plan.md`
- Diagram: `otai-desk-architecture` (the phase-1 drawing was removed)
