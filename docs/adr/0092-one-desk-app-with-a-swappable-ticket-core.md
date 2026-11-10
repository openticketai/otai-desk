---
id: LD-092
status: "superseded by LD-096"
date: 2026-09-29
supersedes: [LD-091]
superseded_by: [LD-096]
legacy_status: Locked
categories: [product-ux, runtime]
---

# One Desk app with a swappable ticket core

## Context and Problem Statement

OTAI Desk had two applications: Nuxt `mini-portal` (facade over OTOBO, Znuny, Zammad, KIX) and Vue `desk` (native ticket system). That split was a mistake. Customers still need both outcomes: run Desk as the helpdesk, or keep the helpdesk they already run. Those are two modes of one application, not two products.

LD-082's one-portal and RBAC rules stay. Its UI path (`mini-portal/frontend`) and the rule that an external ticket system is always the source of record are replaced here. LD-084's product tree stays; its "two apps" path rule is replaced here. LD-090 auth and attributes stay for the Desk API.

## Decision Drivers

- One UI and one API to maintain.
- Desk can be the system of record, or an external ticket system can be.
- External mode keeps a replica; the external system wins conflicts (LD-082 sync rule).
- Do not copy ticket-system groups into Desk RBAC (LD-085).

## Considered Options

- Keep `mini-portal` as a second app for external ticket systems (LD-091).
- Desk-only, and drop "keep your helpdesk".
- **One Desk app. Every use of tickets goes through a ticket-system port. The port's core is Desk or a connector.**

## Decision Outcome

Chosen option: **One application (`line/frontend` + `line/backend`) always talks to a `TicketSystemPort`. The core is local Desk or OTOBO, Znuny, Zammad, or KIX.**

### Binding rules

1. Do not add a second portal app. Do not resurrect Nuxt `mini-portal`.
2. API: `otai-desk/line/backend`. UI: `otai-desk/line/frontend`.
3. `OTAI_DESK_TICKET_PROVIDER` selects the core: `desk`, `otobo`, `znuny`, `zammad`, or `kix`. Unset means `desk` so existing local runs stay the Desk core.
4. `desk`: local Postgres is the source of record. Login uses Desk users and rights (LD-090).
5. Any other provider: `otai-ts-connector` is the core. Desk stores a replica. The ticket system wins conflicts. Until the connector is wired, calls fail loud.
6. External login stamps `customer` or `agent` from the connector identity check. Do not copy ticket-system groups.

## Consequences

- Good: one SPA, one API, two cores.
- Good: "keep your helpdesk" and "Desk is the helpdesk" share the same UI.
- Trade-off: connector writes stay unwired until `otai-ts-connector` is connected; they fail loud.
- Trade-off: LD-082 and LD-084 still describe the older paths; this decision replaces those path and source-of-record sentences.

## Links

- Stable id: `LD-092`
- Supersedes: LD-091
- Related: LD-082, LD-084, LD-085, LD-090
- Diagrams: `otai-desk-architecture` (the backend and phase-1 drawings were removed)
