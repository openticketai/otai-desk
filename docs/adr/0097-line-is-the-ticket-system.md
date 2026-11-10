---
id: LD-097
status: "accepted"
date: 2026-10-30
supersedes: [LD-096]
superseded_by: []
legacy_status: Locked
categories: [product-ux, runtime, deployment]
---

# Line is the ticket system

## Context and Problem Statement

LD-096 rewrote Line onto ASP.NET Core, Razor, and htmx, and it kept a swappable ticket core: Line could store a replica while OTOBO, Znuny, Zammad, or KIX stayed the source of record. That is the portal shape.

Line is the ticket system. Customers and agents use one app. Rights differ. Postgres holds the tickets.

## Decision Drivers

- Line owns tickets, articles, queues, and SLA clocks.
- Login is OIDC. The user directory is LDAP.
- Queue, priority, service, SLA, and escalation follow OTOBO ticket behavior.
- Search is an index. The cache is not a second database.
- Studio ships in the install and stays unpaid until a license is set.
- Razor, htmx, and the `light-forrest` / `dark-forrest` designs from LD-096 stay.

## Considered Options

- Keep `TicketSystemPort` and treat Line as a replica in front of another ticket system.
- Flat roles only (`admin`, `agent`, `employee`) with no policy engine.
- Redis as the SLA store.
- **Line is the ticket system. Casbin decides rights. Keycloak speaks OIDC. LDAP is the directory. Postgres is the source of record. Caddy is the server. Meilisearch is the index. Valkey is the cache.**

## Decision Outcome

Chosen option: **One ASP.NET Core app is the ticket system. Caddy fronts it. Identity is OIDC plus LDAP. Casbin is the rights engine. Postgres, Meilisearch, and Valkey have separate jobs. FAQ, a small CMDB, mail, and an embedded chat are features of this app. Studio is installed beside it and needs a license.**

### Binding rules — app

1. One ASP.NET Core project at `otai-desk/src/`. Razor layouts and partials. htmx returns a partial for ticket list, zoom, reply, and filters. No second SPA. No Vue, Lit, or FastAPI process beside it.
2. The action checks Casbin, then returns HTML for what that user may see. State-changing htmx requests use ASP.NET antiforgery. The browser session is a cookie.
3. Designs stay as in LD-096: `light-forrest` and `dark-forrest`, plus user-created token sets. A design changes colors only.
4. Do not add `TicketSystemPort`. Do not store a replica of OTOBO, Znuny, Zammad, or KIX. Do not resurrect Nuxt `mini-portal`.

### Binding rules — identity and rights

1. Caddy terminates TLS and routes this app, Keycloak, and Studio.
2. Login is OpenID Connect against Keycloak. Keycloak reads the directory from LDAP. Line does not keep a second user directory and does not bind to LDAP on each request.
3. Apache Casbin (Casbin.NET) decides access. Policies persist in Postgres. A queue is a Casbin resource. LDAP groups are not a second rights model. A login may map a directory group onto a Casbin role.
4. Missing session is 401. A Casbin deny is 403.

### Binding rules — data

1. PostgreSQL is the source of record for tickets, articles, queues, priorities, services, SLAs, escalation state, FAQ, CMDB rows, designs, and Casbin policy.
2. Meilisearch indexes tickets, FAQ, and CMDB. It is not the source of record. Rebuild the index from Postgres.
3. Valkey is the cache and speaks the Redis protocol. Use it for short-lived cache and work signals. A Valkey flush must not drop tickets or escalation state. Those stay in Postgres.
4. Do not use Redis server builds. Their license is not the OSS license this product can ship.

### Binding rules — ticket behavior and features

1. A ticket has queue, priority, service, SLA, and escalation times, plus articles. That behavior matches OTOBO. Other facts may be custom attributes.
2. FAQ and a small CMDB are modules in this process. Links between objects are LD-098. No plugin kernel.
3. MailKit fetches IMAP and sends SMTP.
4. Chat embeds into the Razor shell. Line does not run a second agent runtime.
5. OTAI Studio is in the default install, reached through Caddy. It does not unlock until a license is set. Line does not import Studio code. Line runs without that license.

## Consequences

- Good: Line is the helpdesk. Rights, search, and cache have one owner each.
- Good: OIDC and LDAP stay the identity path already used in the demo stack. Casbin covers queue-scoped rights, which three global roles do not.
- Trade-off: LD-096's swappable core, replica rule, and flat role list are retired. The UI rewrite rules live here.
- Trade-off: the older Desk plan drawings (portal, FastAPI, Vue, plugin kernel) are removed. The pictures are `otai-desk-architecture`, `otai-desk-rights`, and `otai-desk-faq-cmdb`.
- Trade-off: Valkey replaces the Redis server the question named. Clients still use the Redis protocol.

## Links

- Stable id: `LD-097`
- Supersedes: LD-096
- Related: LD-082, LD-084, LD-085, LD-098
- Diagrams: `otai-desk-architecture`, `otai-desk-rights`, `otai-desk-faq-cmdb`
