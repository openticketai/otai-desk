---
id: LD-099
status: "accepted"
date: 2026-11-09
supersedes: []
superseded_by: []
legacy_status: Locked
categories: [product-ux, runtime]
---

# OTAI Desk is a modular monolith with module events

## Context and Problem Statement

LD-097 locks the stack: one ASP.NET Core process, Razor and htmx, Casbin, Postgres. It does not say how the code inside that process is cut.

Today one `DeskService` holds login, seed data, designs, tickets, articles, attributes, users, accounts, and FAQ. Each method loads rows, checks a rule, and saves. `TicketRow` has no behavior, so status, SLA, and escalation rules can only live as checks in that service. CMDB, links, reports, and mail would make the same class bigger.

Search, mail, reports, and the Studio Runtime must react when a ticket changes. Called directly, each new reaction is another line in ticket code.

## Decision Drivers

- One process and one deploy stay (LD-097).
- FAQ, CMDB, reports, and other features can be added or switched off without touching ticket code.
- Ticket rules (status, queue, SLA, escalation) live in one tested place.
- A ticket change and its reactions must not drift apart after a crash.
- The boundaries are checked by tests, not by review alone.
- How a module is installed (built in or from Open ITSM Hub) is LD-100's decision, not this one.

## Considered Options

- Keep one service with transaction scripts.
- Microservices per feature.
- Event sourcing with events as the source of record.
- **Modular monolith. One module per feature behind `IDeskModule`. A Ticket aggregate. In-process module events written to a Postgres outbox. Architecture tests for the boundaries.**

## Decision Outcome

Chosen option: **OTAI Desk is a modular monolith. Each feature is a module behind one `IDeskModule` contract. The Tickets module owns a Ticket aggregate. Modules react to each other through events saved in a Postgres outbox. Architecture tests enforce the boundaries.**

### Binding rules — modules

1. Modules: `Tickets`, `Knowledge` (FAQ), `Cmdb`, `Links` (LD-098), `Attributes`, `Reports`, `Mail`, `Identity` (OIDC, Casbin), `Designs`. A new feature is a new module.
2. Each module lives under `src/Modules/<Name>/` and has one class that implements `IDeskModule`. Through it the module registers its services, EF model, HTTP endpoints, and seed data. Navigation entries and Casbin resources join `IDeskModule` when the host stops hard-coding them.
3. `Program.cs` lists the modules built into the app. Whether a module can also be installed from Open ITSM Hub, and how, is LD-100.
4. A module owns its tables. Another module does not read or write them.
5. Another module references only `src/Modules/<Name>/Contracts/`. Contracts hold the public interfaces and event types of that module. Module dependencies have no cycles.
6. Each module owns its `.cshtml` pages and htmx partials under `src/Modules/<Name>/Pages/`. The shell layout stays in the host.
7. Code outside `src/Modules/` that has not moved yet may be called by a module. It does not reference a module, except `Program.cs` and the host setup that register modules.

### Binding rules — ticket model

1. `Tickets` has a `Ticket` aggregate. It owns status changes, queue moves, priority, service, SLA clocks, escalation times, and articles. Pages and API endpoints call ticket use cases. They do not set ticket fields and do not touch the database.
2. Inside `Tickets`, code is split into `Domain/`, `Application/`, `Infrastructure/`, `Pages/`, and `Api/`. `Domain/` does not import EF Core, ASP.NET Core, or code outside the module. The one exception is `src/Domain/`, the shared rules (errors, attribute kinds) with no framework imports.
3. Modules with little behavior (`Knowledge`, `Cmdb`, `Designs`) may keep a use-case class directly over EF. They do not need the same folders.

### Binding rules — events

1. A module raises events for facts other modules need, such as `TicketCreated`, `TicketReplied`, `TicketMoved`, and `EscalationDue`. Event types live in the raising module's `Contracts/`.
2. An event is written to an outbox table in the same Postgres transaction as the change.
3. One dispatcher in the process reads the outbox and calls each subscriber. A subscriber must tolerate the same event twice.
4. Search indexing, mail sending, report counts, and the Studio Runtime webhook are subscribers. Ticket code does not call them directly.
5. Postgres rows stay the source of record. Events are not replayed to rebuild state.

### Binding rules — architecture tests

1. ArchUnitNET tests in `src/tests/Architecture/` enforce the module rules 2, 5, and 7 and the ticket model rules 1 and 2.
2. A change that breaks one of those rules fails `dotnet test`.
3. A new binding rule in this decision gets a test, or the rule says why it cannot be tested.

## Consequences

- Good: CMDB, reports, and later features are added as modules. Ticket code does not grow.
- Good: Ticket rules are in one aggregate and can be tested without a database.
- Good: A crash after saving a ticket does not lose the index update or the Runtime webhook.
- Good: A boundary break shows up as a failing test, not in review.
- Trade-off: `DeskService` has to be split across modules. Until then, modules call it for what has not moved.
- Trade-off: Reactions run after the save, so search can lag a ticket change for a moment.
- Trade-off: The outbox adds one table and one background loop.
- LD-097 stays binding for the stack. Its "no plugin kernel" rule is LD-100's to change. This decision does not decide installation.

## Links

- Stable id: `LD-099`
- Linear: OPE-1447
- Related: LD-097, LD-098, LD-100
- Diagrams: `otai-desk-modules`
