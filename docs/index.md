# OTAI Desk — Documentation

**OTAI Desk** is the ticket system: one ASP.NET Core app, Razor and htmx (LD-097). Postgres is the
source of record. Source code: [github.com/openticketai/otai-desk](https://github.com/openticketai/otai-desk).
Full On-Prem AI still lives in
[OTAI Studio](../../otai/docs/index.md) and
[OTAI Runtime](../../otai/apps/runtime/docs/index.md).

This is the entry point for OTAI Desk. For monorepo-wide context start at the root
[`DOCS.md`](../../DOCS.md).

## Source of truth

- [ADRs](./adr/) — LD-082 one app, LD-084 own tree, LD-097 Line is the ticket system. LD-100 (proposed) is installable plugins, persons, and the privilege tree
- [D2 diagrams](./diagrams/) — [`otai-desk-architecture`](./diagrams/desk/otai-desk-architecture.d2), [`otai-desk-rights`](./diagrams/desk/otai-desk-rights.d2), [`otai-desk-persons`](./diagrams/desk/otai-desk-persons.d2), [`otai-desk-faq-cmdb`](./diagrams/desk/otai-desk-faq-cmdb.d2)
- [TypeSpec](./api-specs/desk-api/main.tsp)
- Phase 1 API: [`../api/`](../api/)

## Handbook

User, administrator, and developer pages are generated from the Razor screens and the HTTP API. Do not edit the locale files by hand.

```powershell
python otai-desk/scripts/handbook.py
```

Sources: [`handbook/`](./handbook/). The Starlight site is [`../docs-site/`](../docs-site/).

## Start here

- [Product decisions](./product-decisions.md) — Line owns tickets. Casbin, OIDC, LDAP. No canned responses.
- [Plugins, persons, and privileges](./requirements-plugins-and-persons.md) — proposed LD-100. Small core, installable plugins, organization-scoped roles.
- [Roadmap](./roadmap.md) — Line as the ticket system, then AI.
- [Phase 1 plan](./phase-1-customer-portal-plan.md) — older portal plan. LD-097 replaces the replica core.
- [Auth capabilities](./ticket-system-capabilities.md) — older per-provider login notes. Identity is now OIDC and LDAP.

## Hard rules

- Treat `data_model.md` as the canonical data model. Update it in the
  same change as any new entity, alongside the relevant Mermaid/PUML
  diagrams.
- Preserve tenant isolation (`org_id` boundaries) in every new entity or
  query.
- OTAI Desk is one ASP.NET Core app under `src/` (LD-097). Razor and htmx. Designs `light-forrest` and `dark-forrest` ship with it.
- **No canned responses** and **no plugin kernel**. See [`product-decisions.md`](./product-decisions.md).

## Related project docs

- [OTAI Studio docs](../../otai-studio/docs/index.md) — control plane
  that produces Desk's categorical models.
- [OTAI Runtime docs](../../open-ticket-ai-runtime/docs/index.md) —
  on-prem inference engine Desk talks to.
- [OTOBO/Znuny client docs](../../otobo-znuny-python-client/docs/index.md)
  — Python connector to the OTOBO/Znuny ticket systems.
