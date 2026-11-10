---
id: LD-085
status: "superseded by LD-090"
date: 2026-09-21
supersedes: []
superseded_by: [LD-090]
legacy_status: Locked
categories: [product-ux, auth]
---

# OTAI Desk owns RBAC; portal mode; own ticket backend later

## Context and Problem Statement

LD-082 one-portal + replica stays. Ticket systems behind `otai-ts-connector`
do not share a rights model. Some have groups and policies; some barely
distinguish customer from agent; some have agents only. Mapping those
groups into Desk would break the connector per provider.

Operators still need one Desk login form that posts to the Desk API. The
browser must never hold ticket-system tokens.

A later optional OTAI ticket backend (demo + developer kernel) must be a
**fifth connector target**, not Desk-as-SoR.

## Decision Drivers

- One portal; rights live in Desk, not in copied TS groups.
- Phase 1 only needs `customer` and `agent`.
- Deployments may be customer-only, agent-only, or both.
- Writes still fail if the connector / ticket system refuses them.
- Casbin is useful later; it is not Phase 1.
- Desk stays a portal. System of record is always a connector target.

## Considered Options

- Mirror TS groups, roles, and policies into Desk.
- Casbin in Phase 1 for every gate.
- Desk as system of record (LD-082 Phase 3).
- **Desk RBAC (`customer`|`agent`) + portal mode; TS identity only; Casbin
  later; own ticket backend as a later connector provider.**

## Decision Outcome

Chosen option: **Desk owns RBAC. The ticket system proves identity and
stores tickets. Phase 1 roles are `customer` and `agent`. Portal mode is
`both` | `customer_only` | `agent_only`. Apache Casbin is postponed.
An OTAI ticket backend, when built, is a new `TicketSystemProvider`.**

LD-082 stays for one UI, replica sync, and fail-loud writes. This decision
**replaces LD-082 Phase 3** (Desk as SoR). Desk never becomes the system of
record.

### Binding rules — auth and roles

1. Login UI posts credentials to Desk (`POST /session`). Desk authenticates
   through the connector. Session is an HTTP-only cookie.
2. `LoginCredentials.as_role` is required (`customer` | `agent`). Desk
   stamps `DeskUser.role` from that path. Do not copy TS groups into Desk.
3. The ticket-system port returns **identity** (`login`, `remote_user_id`),
   not a Desk role.
4. Connector writes remain the hard limit. TS denial → fail loud.

### Binding rules — portal mode and capabilities

1. Config `OTAI_DESK_PORTAL_MODE`: `both` | `customer_only` | `agent_only`.
2. Connector reports `customer_auth` and `agent_auth`.
3. Effective portal = mode ∩ capabilities. `agent_only` or no customer
   auth → Lite Agent UI only; no empty customer shell.
4. `GET /portal` is public so the login screen can choose forms.

### Binding rules — postponed

1. **Casbin:** not Phase 1. Add when queue/field/admin policies outgrow two
   roles. Policies stay in Desk, independent of TS groups.
2. **OTAI ticket backend:** later separate deployable implementing existing
   connector ports. Small admin UI, FAQ, canned replies there are fine.
   Desk portal still has no canned responses.

## Consequences

- Good: one auth story for every provider.
- Good: agent-only ticket systems still work.
- Trade-off: Desk role is not a live mirror of TS rights; connector errors
  still bind writes.
- Agents: `otai-desk/docs/ticket-system-capabilities.md`. The phase-1 drawing was removed. Rights are `otai-desk-rights` (LD-097).

## Links

- Stable id: `LD-085`
- Linear: OPE-1341
- Related: LD-082, LD-084, LD-034
- Plan: `otai-desk/docs/phase-1-customer-portal-plan.md`
