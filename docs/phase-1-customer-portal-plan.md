# OTAI Desk — Phase 1: one app, two ticket cores

Locked by [LD-082](./adr/0082-otai-desk-one-portal-rbac-plus-ticket-system-sync-replica.md),
[LD-085](./adr/0085-otai-desk-owns-rbac-portal-mode-own-ticket-backend.md),
and [LD-092](./adr/0092-one-desk-app-with-a-swappable-ticket-core.md).
Sequencing: [`roadmap.md`](./roadmap.md).

## Goal

Ship an **open-source (LGPL)** **single** app: customers and agents use
`line/frontend`. **Rights** differ. The API always calls a ticket core.
`desk` means local Postgres is the source of record. `otobo`, `znuny`,
`zammad`, or `kix` means that system is the source of record and Desk keeps
a replica.

## Product pitch (one line)

> Run Desk on its own, or keep your helpdesk. One UI for customers and agents.
> Add AI chat when you are ready.

## Architecture summary

The phase-1 drawing was removed. The current picture is [`otai-desk-architecture`](./diagrams/desk/otai-desk-architecture.d2) (LD-097).

```text
Browser → otai-desk/line/frontend
  → otai-desk/line/backend
  → TicketSystemPort
       desk    → local Postgres (source of record)
       otobo|znuny|zammad|kix → replica + otai-ts-connector
                              → ticket system wins conflicts
```

## Open-source boundary

| Component                   | License             | Phase 1 role                |
| --------------------------- | ------------------- | --------------------------- |
| OTAI Desk (API + UI)        | LGPL-2.1            | One portal + replica + sync |
| otai-ts-connector           | Apache-2.0          | Ticket + FAQ I/O            |
| Studio Community Edition    | OSS (TBD SPDX)      | Chatbot config only         |
| apps/chatbot, chat-widget   | Proprietary today\* | Optional Community stack    |
| Runtime + commercial Studio | Proprietary (JWT)   | Paid upgrade                |

\* Source distribution follows LD-029 until explicitly open-sourced.

## Out of scope (Phase 1)

- A second portal application (LD-092)
- Live connector writes (they fail loud until `otai-ts-connector` is wired)
- Full agent workspace, SLA admin, workforce management
- Casbin policies
- Canned responses in the Desk portal
- Runtime classification / summary / reply-draft
- Full Studio AI Task builder
- Migration importers
- Offline / “Desk wins” conflict mode

## Workstreams

### WS1 — Connector surfaces (blocking)

Customer-scoped ops (LD-070) **and** the agent-scoped writes the mini queue
needs (status, optional assignment) — same connector, different credentials
held **only** on the Desk server.

| Capability                               | Notes                                                                      |
| ---------------------------------------- | -------------------------------------------------------------------------- |
| `authenticate_customer` / agent login    | Identity only; Desk stamps role                                            |
| Connector `customer_auth` / `agent_auth` | Matrix: [`ticket-system-capabilities.md`](./ticket-system-capabilities.md) |
| list / get / create / article            | Scoped by DeskRole                                                         |
| status change                            | Agent; if provider supports                                                |
| `search_customer_faq`                    | Public / customer FAQ                                                      |
| sync pull                                | Incremental or windowed fetch                                              |

Providers: **OTOBO + Znuny first**, then Zammad, then KIX.

### WS2 — Replica + sync

1. Persist ticket / article / user-map / FAQ rows with `provider` +
   `remote_id`.
2. **Push** after a user write; fail loud if remote rejects or is down.
3. **Pull** job refreshes replica. Ticket system wins conflicts.
4. Sessions, org branding, audit stay local-only (not synced).

Do **not** default to passthrough-without-rows (retired LD-081 rule).

### WS3 — One portal UI (Vue)

One route tree in `line/frontend`. Hide chrome by permission.

| Surface        | Customer         | Agent (mini)            |
| -------------- | ---------------- | ----------------------- |
| Login          | yes              | yes                     |
| Ticket list    | own tickets      | queue / assigned        |
| Ticket zoom    | visible articles | agent-visible thread    |
| Create / reply | yes              | yes                     |
| Status         | no               | yes if connector writes |
| FAQ            | yes              | yes                     |
| Branding       | org theme        | org theme               |

Embed `OTAIChat.init` when chatbot is enabled — portal works with chat **off**.

### WS4 — Studio Community Edition

Unchanged from LD-081 restatement in LD-082: chatbot config only; Ollama or
OpenRouter; tools FAQ / create_ticket / web_fetch; JWT upgrades in place.

### WS5 — Chatbot stack

Same three tools. Ticket create goes through connector **and** replica.

### WS6 — Compose & distribution

```yaml
services:
  ticket-system: # customer-owned or demo
  desk-api:
  desk-ui:
  chatbot: # optional
  studio-community: # optional
  ollama: # optional
```

Env: `CUSTOMER_INTERFACE=modern|native`, `OTAI_TS_PROVIDER=…`, Studio URL.
OSS README: install without JWT; **Upgrade to Full On-Prem** (LD-047).

### WS7 — Docs & website

- README leads with one portal + sync, not “full helpdesk SoR”.
- Website: Community vs Full On-Prem.

## Milestones

| Milestone                 | Deliverable                                   | Exit criteria                   |
| ------------------------- | --------------------------------------------- | ------------------------------- |
| **M0 — Decision**         | LD-082 + D2 + this plan                       | Accepted ADR, diagram indexed   |
| **M1 — Connector**        | Customer + mini-agent ops (OTOBO/Znuny)       | Integration tests vs demo stack |
| **M2 — Replica sync**     | Push + pull + remote ids                      | No silent diverge; TS wins      |
| **M3 — One portal**       | Login, list, zoom, create, reply; agent queue | Manual QA; chat off             |
| **M4 — Community Studio** | Chatbot config + tool limits                  | Only 3 tools selectable         |
| **M5 — Chat embed**       | Widget on portal                              | FAQ + gated ticket create       |
| **M6 — OSS release**      | Compose + README + website                    | Install without JWT             |

## Success metrics

- Demo: portal on existing OTOBO/Znuny in < 30 minutes
- Zero ticket migration in the sales script
- Customer and agent use the **same** origin / app
- Replica matches remote ticket ids after sync
- Community chatbot can FAQ-answer and escalate to a remote ticket

## Related decisions

- [LD-082](./adr/0082-otai-desk-one-portal-rbac-plus-ticket-system-sync-replica.md)
- [LD-085](./adr/0085-otai-desk-owns-rbac-portal-mode-own-ticket-backend.md)
- [LD-070](../../otai/docs/adr/0070-modern-customer-portal-spa.md) — session /
  no browser TS tokens
- [LD-047](../../otai/docs/adr/0047-unified-license-type-entitlements.md)
- [LD-049](../../otai/docs/adr/0049-agent-tools-in-process-mcp-adapter.md)
