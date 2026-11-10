---
id: LD-100
status: "proposed"
date: 2026-11-09
supersedes: []
superseded_by: []
legacy_status: Locked
categories: [product-ux, runtime]
---

# Hub plugins, persons, and a privilege tree

## Context and Problem Statement

A fresh OTAI Desk install should be a ticket system an admin can learn in one sitting. FAQ and CMDB are real products, and they add a lot of screens. LD-097 keeps both inside the process and forbids a plugin kernel. The proposed LD-099 keeps that ban and compiles every module into the app.

People also need a simpler account model than OTOBO. OTOBO has customer users, customer companies, and a second customer interface. Desk already has one interface. What is missing is how a person, an organization, a queue, and a privilege fit together, and how an OTOBO-style ACL limits a field without becoming a second rights engine.

## Decision Drivers

- The first install shows tickets, people, organizations, queues, and ticket attributes.
- FAQ and CMDB turn on when an admin installs them, and turn off when the admin uninstalls them.
- Install and uninstall ship in the first version. The Open ITSM Hub catalog comes after that.
- A plugin can require another plugin.
- One interface. Privileges decide what that person sees.
- An organization scopes work. It does not grant a privilege by itself.
- A parent privilege includes its children.
- Custom attributes, including every config-item field, are privileges too.
- Conditions restrict a privilege in a situation. They are the ACL.

## Considered Options

- Ship FAQ and CMDB in every install and keep the LD-097 ban on a marketplace.
- Customer users, customer companies, and a second interface, as in OTOBO.
- Flat action rights (`ticket:write`) with no field and no queue.
- Put OTOBO ACLs into the Casbin matcher and expect the matcher to return allowed values.
- **A small core. FAQ and CMDB are plugins the admin installs and uninstalls. The hub catalog comes later. Persons and organizations. Roles grant a privilege tree inside an organization and a queue. Casbin answers yes or no. Condition rows restrict values.**

## Decision Outcome

Chosen option: **A small core. FAQ and CMDB are plugins the admin installs and uninstalls. The hub catalog comes later. Persons and organizations. Roles grant a privilege tree inside an organization and a queue. Casbin answers yes or no. Condition rows restrict values.**

This stays proposed until it replaces the "no plugin kernel" rule in LD-097 and the same ban in LD-099. LD-097 still locks the stack: one ASP.NET Core app, Razor, htmx, Postgres, Casbin.NET, OIDC, LDAP.

### Binding rules — core and plugins

1. The core is persons, organizations, queues, tickets, articles, ticket attributes, roles, privileges, conditions, and the two built-in designs.
2. FAQ is a plugin. Ticket attributes exist without it. FAQ attributes exist when the FAQ plugin is installed.
3. CMDB is a plugin. A config item has a name, a class, and a number. Every other fact is a custom attribute of that class. There is no second kind of custom object.
4. A plugin has a version and a list of plugins it requires. Desk refuses the install until those plugins are installed. Uninstall is refused while another installed plugin requires it.
5. The Plugins screen installs and uninstalls the modules that ship with Desk. Uninstall disables the module. Its rows stay, so a later install shows them again.
6. Open ITSM Hub is the later catalog. The Plugins screen and the Skins screen list community packages from the hub after the first version. A hub skin is a named token set. `light-forrest` and `dark-forrest` stay built in. A skin does not change markup or privileges.
7. Links stay LD-098. A ticket links to a FAQ entry or a config item when that plugin is installed.

### Binding rules — persons

1. A person is the account. An admin, an agent, a customer, an AI agent, and a system user are persons whose roles differ.
2. An organization is a company or a department. Membership says a person belongs to an organization and may store a position. The position is a label on the ticket. It grants nothing.
3. A role is a named set of privileges. A role assignment gives one person one role inside one organization. The same person can hold different roles in different organizations.
4. The organization does not add privileges. The assignment makes the role count only inside that organization.

### Binding rules — privileges

1. A privilege is a node in a tree. A grant of a parent includes every child.
2. The tree is rooted at objects: ticket, person, organization, queue, and, when installed, FAQ and config item class.
3. Each attribute is a privilege under its object, with two children: `view` and `edit`. A custom attribute is added to the tree when the attribute is created.
4. Changing a ticket is editing an attribute. State, queue, priority, and an internal article are attributes. There is no separate close or move privilege.
5. A queue grant stores organization, role, queue, and privilege. A person with that role in that organization receives those privileges on that queue.
6. Casbin.NET answers `Enforce(person, organization, queue, privilege)`. A parent grant satisfies a child check. Missing session is 401. A deny is 403.

### Binding rules — conditions

1. A condition is a row: organization, optional role, optional queue, optional state, privilege, effect (`allow`, `deny`, or `values`), and the value list when the effect is `values`.
2. Casbin runs first. A condition then denies an allowed privilege, or limits the values of an attribute. The value list is not a Casbin result.
3. Conditions are the OTOBO ACL. They do not create roles.

## Consequences

- Good: a new install is the ticket system. FAQ and CMDB appear when an admin installs them and disappear when the admin uninstalls them.
- Good: one person model covers customers, agents, AI agents, and system users.
- Good: a person who can see every ticket in one organization still cannot see another organization.
- Trade-off: LD-097 and LD-099 forbid this marketplace until this decision is accepted.
- Trade-off: queue grants are per role and organization. A narrower person gets another role.
- Trade-off: two checks run on a write, Casbin and then the condition rows.

## Links

- Stable id: `LD-100`
- Linear: OPE-1448
- Requirements: `otai-desk/docs/requirements-plugins-and-persons.md`
- Related: LD-097, LD-098, LD-099
- Diagrams: `otai-desk-persons`
