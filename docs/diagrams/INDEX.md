---
name: OTAI Desk D2 Diagrams Index
id: otai-desk-diagrams-index
description: >-
  D2 architecture, persistence, and API surface diagrams for OTAI Desk.
  Source of truth alongside otai-desk/docs/adr/ and desk-api TypeSpec.
project_name: OTAI Desk
title: OTAI Desk D2 Diagrams Index
tags:
  - diagrams
  - architecture
  - d2
  - index
---

# OTAI Desk D2 Diagrams Index

In **OTAI Desk**, D2 diagrams are a primary source of truth alongside accepted ADRs ([`docs/adr/`](../adr/)) and TypeSpec ([`docs/api-specs/desk-api/main.tsp`](../api-specs/desk-api/main.tsp)). See [`docs/index.md`](../index.md) and [`docs/product-decisions.md`](../product-decisions.md).

- **Phase 1 line:** plan diagrams use `status: plan-feature` and `version` in frontmatter ([LD-078](../adr/), [LD-084](../adr/)). Promote to `current` in place — do not rename files.
- **Diagram sources**: [`.d2`](./) files in domain subfolders.
- **Render pipeline**: Rendered to high-resolution `.png` via `render_diagrams.py` (temporary SVG is discarded).
- **Diagram metadata**: Standardized commented YAML frontmatter at the top of each `.d2` file.
- **Token counts** (`cl100k_base`, same family Cursor uses): per `.d2` in the tables below — use when budgeting `@` attachments and agent reads.

**Catalog size:** 5 diagrams · **4,681 tokens** if every `.d2` source is attached (~4.7k compact).

---

## Quick Navigation by Domain

- [OTAI Desk](#otai-desk) (5 diagrams)

---

## <a id="otai-desk"></a>OTAI Desk

| Diagram | Status | Tokens | Description | Tags / ADRs | Formats |
| --- | --- | --- | --- | --- | --- |
| **OTAI Desk Architecture**<br>[`otai-desk-architecture.d2`](./desk/otai-desk-architecture.d2) | `plan-feature` (2.0.0) | `963` | OTAI Desk is the ticket system (LD-097). ASP.NET Core, Razor, and htmx. Caddy in front. Keycloak OIDC and LDAP for identity. Casbin for rights. Postgres is the source of record. Meilisearch is the index. Valkey is the cache. FAQ, CMDB, IMAP/SMTP, embedded chat, and licensed Studio. | `otai-desk`, `architecture`, `aspnet`, `casbin`<br>ADRs: [LD-082](../adr/), [LD-097](../adr/) | [`PNG`](./desk/otai-desk-architecture.png) |
| **OTAI Desk FAQ, CMDB, and links**<br>[`otai-desk-faq-cmdb.d2`](./desk/otai-desk-faq-cmdb.d2) | `plan-feature` (2.0.0) | `905` | FAQ, configuration items, and OTOBO-style links (LD-098). One link row joins any two objects and carries a type. Custom attributes hang off tickets, FAQs, configuration items, and links. | `otai-desk`, `faq`, `cmdb`, `links`<br>ADRs: [LD-097](../adr/), [LD-098](../adr/), [LD-101](../adr/), [LD-090](../adr/) | [`PNG`](./desk/otai-desk-faq-cmdb.png) |
| **OTAI Desk Modules**<br>[`otai-desk-modules.d2`](./desk/otai-desk-modules.d2) | `plan-refactor` (2.1.0) | `~1.2k` | Code shape inside the one OTAI Desk process (LD-099). Modular monolith: one module per feature behind IDeskModule. Tickets owns a Ticket aggregate and is split into Domain, Application, Infrastructure, Pages, and Api. Ticket events live in Tickets Contracts and go through a Postgres outbox. Search, mail, reports, and the Studio Runtime webhook subscribe. ArchUnitNET tests guard the boundaries. | `otai-desk`, `architecture`, `modules`, `events`<br>ADRs: [LD-097](../adr/), [LD-098](../adr/), [LD-099](../adr/) | [`PNG`](./desk/otai-desk-modules.png) |
| **OTAI Desk Persons**<br>[`otai-desk-persons.d2`](./desk/otai-desk-persons.d2) | `plan-feature` (2.0.0) | `~1k` | Person, organization, role assignment, privilege tree, and queue grant (LD-100). A position stores a label. An organization scopes a role. A parent privilege includes its children. Conditions run after Casbin. | `otai-desk`, `persons`, `privileges`, `casbin`<br>ADRs: [LD-100](../adr/), [LD-097](../adr/) | [`PNG`](./desk/otai-desk-persons.png) |
| **OTAI Desk Rights**<br>[`otai-desk-rights.d2`](./desk/otai-desk-rights.d2) | `plan-feature` (2.0.0) | `619` | Apache Casbin (Casbin.NET) for OTAI Desk (LD-097). Policies persist in Postgres. A queue is the object. An LDAP group maps onto a Casbin role at login. Missing session is 401. A deny is 403. | `otai-desk`, `casbin`, `rights`, `oidc`<br>ADRs: [LD-097](../adr/) | [`PNG`](./desk/otai-desk-rights.png) |

---

## AI Agent Guidance: Selecting & Inspecting Diagrams

When implementing features or refactoring:
1. **Resolve one id**: `uv run otai-desk/scripts/diagrams.py search <topic>` then `show <id>` (do not attach this INDEX or the diagrams folder).
2. **Inspect that `.d2` only**: Read the printed path, or `show <id> --body` for compact structure.
3. **Follow Class/Method Signatures**: Persistence models, method signatures, and router routes drawn in plan diagrams are authoritative specification contracts.

To regenerate this index or re-render diagrams:
```bash
uv run otai-desk/scripts/diagrams.py show otai-desk-architecture
uv run otai-desk/docs/diagrams/render_diagrams.py --index-only  # Rebuild index without rendering
uv run otai-desk/docs/diagrams/render_diagrams.py               # Rebuild index and render all PNG
```
