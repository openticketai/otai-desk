---
id: LD-091
status: "superseded by LD-092"
date: 2026-09-29
supersedes: []
superseded_by: [LD-092]
legacy_status: Locked
categories: [product-ux, auth]
---

# Mini-portal facade for external ticket systems; unified desk frontend for OTAI Desk

## Context and Problem Statement

`otai-desk` contains two distinct frontend trees: `otai-desk/mini-portal/frontend` (Nuxt 3) and `otai-desk/line/frontend` (Vue 3). There was architectural ambiguity about whether `mini-portal` should serve as the customer portal for the native `otai-desk/line/backend` ticket system, or whether OTAI Desk should follow the legacy OTOBO/Znuny split of having distinct user accounts and distinct frontend portals.

OTOBO and Znuny enforce a rigid split between agents (`users`) and customers (`customer_user` + `customer_company`). This causes significant friction in internal IT/ESM service desks where agents are also ticket requesters, doubles authentication and session handling, and creates fractured portals. Furthermore, maintaining two frontend applications for a single native ticket system doubles UI maintenance overhead.

## Decision Drivers

- Legacy ticket systems (OTOBO, Znuny, Zammad, KIX) need a modern, lightweight customer portal without requiring customers to replace their backend.
- OTAI Desk as an independent ticket system needs modern, unified identity and frictionless role transitions (agents can request tickets, customers can be promoted to agents).
- Avoid maintaining two separate frontends for the OTAI Desk product.
- Enable standalone, lightweight sales demos of the `mini-portal` without needing heavy 3rd-party Docker/Perl stacks.

## Considered Options

- Use `mini-portal` as the customer portal for OTAI Desk, maintaining a separate agent frontend.
- Adopt the OTOBO/Znuny split (`users` vs `customer_user`) across the OTAI Desk backend and frontend.
- **`mini-portal` serves exclusively as a facade for external ticket systems (and test/demo driver); OTAI Desk uses a single unified frontend (`line/frontend`) with role-based views and unified identity.**

## Decision Outcome

Chosen option: **`mini-portal` is a universal portal facade for 3rd-party ticket systems (OTOBO, Znuny, Zammad, KIX) and demo driver; the native OTAI Desk ticket system uses a single unified frontend (`line/frontend`) with role-based navigation and permissions under a unified user model.**

The OTAI Desk backend (`line/backend`) implements connector provider compatibility so sales demos can showcase `mini-portal` over an instant, lightweight local backend without spinning up legacy systems.

### Binding rules — Mini-Portal (`otai-desk/mini-portal`)

1. `mini-portal` is targeted at customers who retain their existing external ticket systems (OTOBO, Znuny, Zammad, KIX). It communicates via `otai-ts-connector` and portal replica sync (LD-082).
2. `mini-portal` is NOT the customer interface for native OTAI Desk deployments.
3. For sales demos and integration tests, `line/backend` serves as an optional mock/provider target for `mini-portal`.

### Binding rules — OTAI Desk Frontend (`otai-desk/line/frontend`)

1. OTAI Desk has exactly one frontend application: `otai-desk/line/frontend`. All users (customers, employees, agents, administrators) log into this same SPA.
2. User identity is unified (LD-090). There is no `customer_user` table or separate customer authentication route.
3. Visible navigation, queue access, actions, and screens adapt dynamically based on the user's roles and FastAPI Security scopes returned at login:
   - Users with customer/employee rights see their submitted requests, company requests, and knowledge base.
   - Users with agent/admin rights see triage queues, SLA clocks, assignment controls, internal note toggles, and system settings.
4. Internal employees and agents can submit requests as customers in the same interface without dual accounts, secondary logins, or domain switching.

## Consequences

- Good: eliminates code and UI duplication; single design system and router for OTAI Desk.
- Good: agents can seamlessly file IT/HR tickets as requesters without logging out or needing dummy accounts.
- Good: `mini-portal` remains clean, dedicated, and decoupled as a lightweight modernization layer for legacy helpdesk customers.
- Good: instant, self-contained sales demos of `mini-portal` using `line/backend` without running external Docker stacks.
- Trade-off: `line/frontend` must implement robust permission-based route guards and conditional UI rendering for customer vs agent views.

## Links

- Stable id: `LD-091`
- Related: LD-082, LD-084, LD-085, LD-090
- Diagrams: `otai-desk-architecture` (the backend and phase-1 drawings were removed)
