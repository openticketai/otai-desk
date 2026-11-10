---
id: LD-084
status: "accepted"
date: 2026-09-20
supersedes: [LD-083]
superseded_by: []
legacy_status: Locked
categories: [process, product-ux]
---

# OTAI Desk SSOT lives in otai-desk

## Context and Problem Statement

LD-083 put Desk apps and Desk specs under `otai/`. Desk is a separate
product (portal + replica, no AI in Phase 1). Same methodology as OTAI
does not require the same folder. Studio embeds later via chat-widget
in the Desk UI, not as a Desk module.

## Decision Drivers

- Own deployable and LGPL tree.
- Agents must still follow ADR → D2 → TypeSpec → code.
- `otai/` stays Studio / Runtime / Chatbot.

## Considered Options

- Keep Desk in `otai/apps/desk-*` (LD-083).
- **Own tree `otai-desk/` with its own docs, TypeSpec, D2, and ADRs.**

## Decision Outcome

Chosen option: **OTAI Desk lives in `otai-desk/`. Desk ADRs, D2, TypeSpec,
and Phase 1 API code stay there.**

LD-083 is superseded. LD-082 product rules stay (file now in
`otai-desk/docs/adr/`).

### Binding rules

1. **Do not** add Desk features under `otai/apps/`.
2. Desk SSOT: `otai-desk/docs/adr/`, `otai-desk/docs/diagrams/`,
   `otai-desk/docs/api-specs/`.
3. One application (LD-092): API `otai-desk/line/backend`, UI `otai-desk/line/frontend`. Ticket core is `OTAI_DESK_TICKET_PROVIDER` (`desk` or `otobo` / `znuny` / `zammad` / `kix`). Do not add a second portal app.
4. Same order as LD-078, inside that tree. No TypeSpec codegen.
5. Optional Studio/chat-widget is an embed into Desk UI, not a shared
   process with Studio API.

## Consequences

- Good: product and license boundaries stay clear.
- Desk reuses the same CLIs: `otai-desk/scripts/adr.py` and
  `diagrams.py` (or `otai/scripts/*.py --product desk`). LD-\* `next-id`
  still scans both `otai/docs/adr/` and `otai-desk/docs/adr/`.

## Links

- Stable id: `LD-084`
- Linear: OPE-1338
- Desk ADRs: `otai-desk/docs/adr/`
- Supersedes: [LD-083](./0083-otai-desk-lives-in-otai-with-adr-d2-typespec-ssot.md)
