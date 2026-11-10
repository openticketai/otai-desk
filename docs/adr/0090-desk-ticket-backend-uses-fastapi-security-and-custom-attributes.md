---
id: LD-090
status: 'superseded by LD-096'
date: 2026-09-28
supersedes: [LD-085]
superseded_by: [LD-096]
legacy_status: Locked
categories: [auth, product-ux]
---

# Desk ticket backend uses FastAPI Security and custom attributes

## Context and Problem Statement

`otai-desk/line/backend` is the ticket system. The Desk API in
`otai-desk/docs/api-specs/desk-api/main.tsp` still described a portal:
`portal_mode`, `as_role`, and roles `customer` | `agent`. A ticket backend
does not choose a frontend mode. Callers need users, roles, and rights.
Tickets need new fields (queue, priority, type, service, and later ones)
without a schema change for each field.

LD-085 postponed Casbin and kept the ticket system behind a connector as
the system of record. That does not fit this backend.

## Decision Drivers

- Authorization is FastAPI `Security` with bearer scopes, not a portal cookie and not Casbin.
- Rights are a small fixed set. Roles grant rights. Users have roles.
- A ticket row stores identity, title, and the workflow fields agents filter on. Other ticket facts are custom attributes.
- Each custom attribute has a definition (name, kind, required, options).
- An article stores an id and a meta object. It is not a second attribute system.
- Mini-portal UI mode is outside this API.

## Considered Options

- Keep `portal_mode` and `customer` | `agent` on this API.
- Apache Casbin for every check.
- Fixed columns for queue, priority, type, service, and status.
- **FastAPI Security scopes, role-to-right grants, and defined custom attributes on tickets.**

## Decision Outcome

Chosen option: **The Desk ticket backend authenticates with OAuth2 password
bearer tokens. Scopes are rights. Tickets store identity, title, and the
workflow fields. Further facts are attribute values. Articles are id plus meta.**

LD-082 stays for one portal and replica sync. LD-092 is the one app and the swappable ticket core. This decision replaces
LD-085 for `line/backend` and for `desk-api/main.tsp`.

### Binding rules — auth

1. `POST /api/v1/token` accepts `OAuth2PasswordRequestForm` (`username`, `password`).
2. The access token lists the user's rights. Rights are the union of rights on the user's roles.
3. Routes use `Security(get_current_user, scopes=[...])`. Missing token is 401. Missing scope is 403.
4. Seed roles `admin`, `agent`, and `employee`. Seed rights `tickets:read`, `tickets:write`, `attributes:manage`, and `users:manage`.
5. Do not expose `PortalMode`, `as_role`, or `GET /portal` on this API.

### Binding rules — tickets

1. Ticket columns that are always present: `id`, `title`, `ticket_number`, `status`. `organization_id` is tenancy, not an attribute. `queue`, `priority`, `type`, and `service` are columns and may be empty.
2. Custom attributes are facts the customer defines. They are not `status`, `queue`, `priority`, `type`, or `service`.
3. In code, a definition's kind is `str`, `int`, `bool`, `datetime`, `UUID`, or a `SelectKind` model that carries its options. The row stores that as `string`, `number`, `boolean`, `datetime`, `user`, or `select`.
4. `TicketAttributeValue` stores one string value per definition on a ticket. The service checks the value against `value_kind`.
5. Ticket JSON is `{ id, ticket_number, title, status, queue, priority, type, service, attributes }` where `attributes` maps definition name to value.
6. There is no status-only route. Status is the `status` column.

### Binding rules — articles

1. An article has `id`, `ticket_id`, and `meta`.
2. `meta` holds `body`, `subject`, `from`, `to`, `content_type`, `sender_type`, `created_at`, `author_user_id`, and `is_internal`.
3. Body is not its own column.

## Consequences

- Good: customer-defined fields are definitions and values, not migrations of the ticket row.
- Good: queue and status filters are columns.
- Good: one authorization mechanism for every route.
- Trade-off: a new workflow field still needs a migration. Only extra facts stay in attributes.
- Trade-off: LD-085 portal mode does not apply to this API. There is one UI, `line/frontend` (LD-092).

## Links

- Stable id: `LD-090`
- Linear: OPE-1355
- Related: LD-082, LD-084, LD-085
- Spec: `otai-desk/docs/api-specs/desk-api/main.tsp`
