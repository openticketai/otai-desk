---
id: LD-096
status: "superseded by LD-097"
date: 2026-10-30
supersedes: [LD-090, LD-092]
superseded_by: [LD-097]
legacy_status: Locked
categories: [product-ux, runtime]
---

# Line rewrites onto ASP.NET Core with Razor, htmx, and user designs

## Context and Problem Statement

Line is one ticket app. Customers and agents use it together; rights differ. The running tree is a FastAPI process in `line/backend` and a Vue SPA in `line/frontend` (LD-090, LD-092). That split puts rights, layout, and ticket screens in the browser.

The app also needs designs in the OTOBO skin sense: a named look the user can choose, plus looks the user creates. Two designs ship with the product: `light-forrest` and `dark-forrest`.

## Decision Drivers

- One process renders the screen and enforces rights.
- Ticket list, zoom, reply, and filters are fragments of that screen.
- The FastAPI app and the Vue SPA are rewritten. They do not stay beside the new app.
- A design is a skin. Users can create their own. `light-forrest` and `dark-forrest` are built in.
- The swappable ticket core, ticket columns, custom attributes, and article meta from LD-090 and LD-092 stay.

## Considered Options

- Keep FastAPI and add htmx beside the Vue SPA.
- ASP.NET Core with a Lit (or other) client SPA.
- User-uploaded stylesheets, as a raw OTOBO skin directory.
- **Rewrite Line as one ASP.NET Core app. Razor renders HTML. htmx swaps fragments. Designs are named token sets. `light-forrest` and `dark-forrest` ship with the app, and a user can create more.**

## Decision Outcome

Chosen option: **One ASP.NET Core app replaces `line/backend` and `line/frontend`. Razor and htmx are the UI. Designs are skins. `light-forrest` and `dark-forrest` are the built-in designs, and users create their own.**

### Binding rules — rewrite

1. Rewrite the existing Line code into one ASP.NET Core project at `otai-desk/src/`. Remove the FastAPI app and the Vue SPA in that rewrite. Do not keep either as a second process.
2. Razor layouts and partial views render the UI. htmx requests return a partial. Ticket list, zoom, reply, and filters are partials.
3. The action checks rights, then returns HTML for what that user may see. The browser does not decide rights, menus, or layout.
4. State-changing htmx requests use ASP.NET antiforgery. The browser session is a cookie.
5. Studio and other machine clients call the same app with `Authorization: Bearer`. Policies are the rights. Missing cookie or token is 401. Missing right is 403.
6. Do not add Lit, Vue, or a second frontend project.

### Binding rules — designs

1. A design is a skin: a name plus a fixed set of color tokens. The server turns those tokens into CSS variables on the layout. A design does not change markup, routes, or rights.
2. Built-in design ids are `light-forrest` and `dark-forrest`. Both ship in the app. Users can select either. Neither can be deleted or overwritten.
3. A signed-in user can create further designs and select among the built-ins and the designs they created. The selected design id is stored on the user. Creating a user requires that id.
4. A user cannot select or edit another user's design. A missing or unknown design id fails the request.
5. Users do not upload a stylesheet or HTML. They set the same tokens the built-in designs set.

### Binding rules — carried forward

1. One app. Do not add a second portal. Do not resurrect Nuxt `mini-portal`.
2. `OTAI_DESK_TICKET_PROVIDER` selects the core: `desk`, `otobo`, `znuny`, `zammad`, or `kix`. Unset means `desk`.
3. `desk`: Postgres is the source of record. Roles are `admin`, `agent`, and `employee`. Rights are `tickets:read`, `tickets:write`, `attributes:manage`, and `users:manage`. A role grants rights. The user's rights are the union.
4. Any other provider: `otai-ts-connector` is the core. Line stores a replica. The ticket system wins conflicts. Until the connector is wired, calls fail loud. External login stamps `customer` or `agent` from the connector. Do not copy ticket-system groups.
5. Ticket columns that are always present: `id`, `title`, `ticket_number`, `status`. `organization_id` is tenancy. `queue`, `priority`, `type`, and `service` are columns and may be empty. Other ticket facts are custom attributes with a definition (name, kind, required, options).
6. An article has `id`, `ticket_id`, and `meta`. `meta` holds `body`, `subject`, `from`, `to`, `content_type`, `sender_type`, `created_at`, `author_user_id`, and `is_internal`.

## Consequences

- Good: rights and layout stay on the server. The screen is HTML fragments.
- Good: `light-forrest` and `dark-forrest` are real skins, and a user can add their own without a second frontend.
- Good: one deployable replaces the Python API and the Vue SPA.
- Trade-off: LD-090 and LD-092 are retired. Attribute, article, and swappable-core rules live in this decision.
- Trade-off: the Desk D2 plans and `desk-api` TypeSpec still describe FastAPI and the Vue SPA. Redraw them to this decision before writing the ASP.NET app.
- Trade-off: a user design can change colors only. It cannot restyle structure the way a hand-written OTOBO CSS file can.

## Links

- Stable id: `LD-096`
- Linear: OPE-1416
- Supersedes: LD-090, LD-092
- Related: LD-082, LD-084, LD-085
