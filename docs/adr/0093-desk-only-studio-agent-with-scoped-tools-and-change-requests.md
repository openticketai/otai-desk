---
id: LD-093
status: "proposed"
date: 2026-09-30
supersedes: []
superseded_by: []
legacy_status: Locked
categories: [product-ux, runtime, auth]
---

# Connector capabilities for Desk; Studio stays a paid client

## Context and Problem Statement

OTAI Desk is LGPL. Commercial Studio + Runtime are paid (LD-029). They are
not connected yet. Desk must not import or embed Studio.

Studio already reaches ticket systems only through `otai-ts-connector`.
Desk can implement operations other providers cannot. Those extras still
have to be connector methods + capability flags, not a second Desk-only
HTTP tool bus.

A second AI runtime inside Desk (LangGraph, pydantic-ai, …) would
duplicate Runtime and split the product.

## Decision Drivers

- One I/O path: `otai-ts-connector`.
- OSS Desk must stay usable without buying Studio.
- Desk can advertise more capabilities than OTOBO / Znuny / Zammad / KIX.
- Approval and access policy belong in Desk.
- Do not maintain two agent engines.

## Considered Options

- Embed Studio / Runtime in the Desk tree (license leak).
- Studio talks to the Desk API and skips the connector.
- A second OSS agent framework inside Desk.
- **Studio (paid) is a client of the connector. Desk is a provider with
  extra capabilities. ChangeRequest + access live in Desk. No second
  agent engine.**

## Decision Outcome

Chosen option: **All ticket-system I/O goes through `otai-ts-connector`.
Desk is a provider that reports extra capabilities. Studio uses those
when present. Desk does not embed Studio. Desk does not grow its own
agent framework.**

Humans still use Desk’s `TicketSystemPort` (LD-092). That port is the
Desk-side adapter over the same connector (local `desk` core or remote
TS). Studio never calls Desk routers for tools.

### Binding rules — license and products

1. Desk source does not depend on Studio or Runtime packages.
2. Pairing Studio with Desk is optional and paid. Desk works alone.
3. Do not add LangGraph / pydantic-ai / a second workflow runner in Desk.

### Binding rules — connector

1. Studio Runtime keeps one ticket client: `otai-ts-connector`.
2. Add `desk` as a connector provider (HTTP to Desk, or in-process in
   combined deploys). Same ports as other providers.
3. Capabilities are explicit (`change_request`, richer FAQ, attributes,
   …). Missing capability → `UnsupportedTicketSystemCapabilityError`.
   OTOBO / Znuny / Zammad / KIX leave Desk-only flags off.
4. Access `deny` | `execute` | `request_approval` is Desk config. The
   connector exposes it; Studio does not store a second ACL.

### Binding rules — trigger and approval

1. `ticket-changed` (existing Runtime trigger) is enough. Stamp agent
   actor; skip self.
2. `ChangeRequest` is a Desk resource. Connector has create / list /
   resolve. Approve applies through the same write path.

## Consequences

- Good: one integration story for every ticket system, including Desk.
- Good: Desk stays OSS; Studio remains the paid brain.
- Good: richer tools are capability rows, not a special agent kind hop.
- Trade-off: proposed until connector capability catalog + TypeSpec exist.
- Trade-off: combined “Desk + remote TS” still means Studio talks to
  **Desk-as-provider** if you want Desk extras; talking to Znuny directly
  stays the thin set.

## Links

- Stable id: `LD-093`
- Related: LD-029, LD-090, LD-092
- Linear: OPE-1360
- Diagrams: `otai-desk-architecture` (the studio-agent drawing was removed)
