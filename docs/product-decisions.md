# OTAI Desk — Product Decisions

Locked product direction that constrains scope. When docs, specs, or code
disagree with this file, update them to match — do not silently expand scope.

Architecture lock: [LD-097](./adr/0097-line-is-the-ticket-system.md). Line is the ticket system. ASP.NET Core, Razor, htmx, Casbin, OIDC, LDAP, Postgres, Caddy, Meilisearch, Valkey.

Proposed, not locked: [LD-100](./adr/0100-hub-plugins-persons-and-a-privilege-tree.md) and [plugins, persons, and privileges](./requirements-plugins-and-persons.md). A small core, FAQ and CMDB as plugins the admin installs and uninstalls, the Open ITSM Hub catalog after the first version, persons and organizations, and a privilege tree. The "no plugin kernel" section below stays until LD-100 is accepted.
Location: [LD-084](./adr/0084-otai-desk-ssot-lives-in-otai-desk.md). LD-085's portal mode and replica rules are retired by LD-097.

## One portal; rights, not two apps

There is **one** Desk UI. Customers and agents are the same product with
different permissions. Do not design a separate customer-portal and
agent-portal.

Ticket behavior matches OTOBO: queue, priority, service, SLA, escalation times, and articles.

## Line owns tickets and rights

Login is OpenID Connect (Keycloak). LDAP is the directory behind Keycloak.
**Casbin** (Casbin.NET) decides access. Picture: [`otai-desk-rights`](./diagrams/desk/otai-desk-rights.d2). Policies live in Postgres. A queue is the object. `Enforce(subject, queue, action)` runs before HTML. An LDAP group maps onto a Casbin role at login.

Postgres is the source of record. Meilisearch is the search index. Valkey is the cache.
FAQ and a small CMDB are modules in this app. Picture: [`otai-desk-faq-cmdb`](./diagrams/desk/otai-desk-faq-cmdb.d2). Links are [LD-098](./adr/0098-line-links-any-object-with-a-type-and-attributes.md): one row joins any two objects and carries a type. Custom attributes apply to tickets, FAQs, configuration items, and links. Mail (IMAP fetch, SMTP send) is a module in this app.
Chat embeds into the UI. OTAI Studio is in the default install and stays locked until a license is set.

## No plugin kernel

Desk features are ordinary modules in one process. A plugin microkernel,
extension SDK, and marketplace are not the architecture.

What matters is the **AI-native ticket core**:

- tickets and conversations as the workflow surface
- human **and** agent actors with scoped rights
- approval gates for risky agent actions
- immutable audit for compliance (Betriebsrat, GDPR)

## No canned responses

**Canned responses / macros are not a target feature.**

They are a pre-AI helpdesk pattern: admins maintain a library of static reply
snippets; agents search and paste them. That made sense when every reply was
typed from scratch.

In an AI-first product there is no good reason for canned responses:

- Agents already get **context-aware suggested replies** grounded in the
  knowledge base and ticket history (see Phase 2 in [`roadmap.md`](./roadmap.md)).
- Static templates go stale, need categorisation, and duplicate what RAG + LLM
  can generate per ticket.
- Maintaining a canned-response library is admin overhead with little payoff
  when AI drafts appropriately for each case.

**Decision:** Do not design, spec, or market canned responses in the Desk
portal. Any existing API or UI scaffold is provisional and should not be
expanded; remove when convenient. A later OTAI ticket-backend admin UI may
keep canned replies there — that is not Desk.
