---
id: LD-083
status: "superseded by LD-084"
date: 2026-09-20
supersedes: []
superseded_by: [LD-084]
legacy_status: Locked
categories: [process, product-ux]
---

# OTAI Desk lives in otai with ADR D2 TypeSpec SSOT

## Context and Problem Statement

OTAI Desk Phase 1 product rules live in LD-082, but implementation still
sat in the top-level `otai-desk/` tree with Markdown plans as the daily
source of truth. Other product apps live under `otai/apps/` and are
driven by accepted ADRs → D2 → TypeSpec → code (LD-078). Agents invent
Desk routes and tables from the old FastAPI/Nuxt tree (including canned
responses that LD-082 forbids).

## Decision Drivers

- One Clean Architecture tree for Desk next to Studio, Runtime, Chatbot.
- Agents must not invent HTTP or persistence from as-is `otai-desk/` code.
- LD-082 product rules stay; only **location and SSOT stack** change.

## Considered Options

- Keep developing in `otai-desk/` and copy diagrams later.
- Start `apps/customer-portal-*` (forbidden by LD-082).
- **New Desk apps under `otai/` with ADR + D2 class/API diagrams + TypeSpec as SSOT.**

## Decision Outcome

Chosen option: **OTAI Desk is developed in `otai/`. Accepted ADRs, D2
plan/class/API-surface diagrams, and TypeSpec are the source of truth;
code is written to match them.**

LD-082 still binds one portal, RBAC, replica sync, and ticket-system SoR.

### Binding rules

1. **Code home:** Phase 1 API is `otai/apps/desk-api`. Portal UI is
   `otai/apps/desk-web` (one SPA). Do not add features in `otai-desk/`
   except a pointer README. Do not start `apps/customer-portal-*`.
2. **SSOT order (LD-078):** accepted ADR → matching D2 →
   `otai/docs/api-specs/desk-api/main.tsp` → tests → hand-written code.
   No TypeSpec codegen. The drawings named here (`otai-desk-phase-1-plan`,
   `otai-desk-db-models`, `otai-desk-api-surface`) were removed. Current
   pictures: `otai-desk-architecture`, `otai-desk-rights`, `otai-desk-faq-cmdb`.
3. **Layers:** desk-api uses the numbered Clean Architecture stack
   (inbound adapters → inbound ports → application → domain → outbound
   ports → outbound adapters → SQLModel rows). Import-linter + Ruff
   like other server apps (LD-059).
4. **HTTP:** every route and wire model must exist in TypeSpec first.
   Extra endpoints from the old `otai-desk` tree (canned responses,
   SLA admin) are **not** ported.
5. **Persistence:** table shapes come from the current D2
   (`otai-desk-faq-cmdb`), not from `otai-desk/docs/data_model.md`.
   `otai-desk-db-models` was removed.
6. **Connector:** ticket I/O stays `otai-ts-connector` (LD-034 / LD-082).
   Writes fail if the ticket system is down.

## Consequences

- Good: Desk follows the same agent workflow as Studio/Runtime.
- Good: old `otai-desk` scaffolds cannot silently become the product.
- Trade-off: existing Nuxt/FastAPI under `otai-desk/` is legacy until
  `desk-web` is implemented from the same specs.

## Links

- Stable id: `LD-083`
- Linear: OPE-1337
- Product rules: [LD-082](./0082-otai-desk-one-portal-rbac-plus-ticket-system-sync-replica.md)
- SSOT stack: [LD-078](./0078-adr-d2-typespec-are-architecture-ssot.md)
- TypeSpec: `otai/docs/api-specs/desk-api/main.tsp`
- Diagrams: `otai-desk-architecture`, `otai-desk-rights`, `otai-desk-faq-cmdb`
