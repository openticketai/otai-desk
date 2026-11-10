---
title: OTAI Desk plugins, persons, and privileges
description: Requirements and todos for a small Desk core, installable plugins, and organization-scoped privileges.
version: "2.0.0"
last_updated_at: "2026-11-09"
---

# Plugins, persons, and privileges

Working requirements for [LD-100](./adr/0100-hub-plugins-persons-and-a-privilege-tree.md). Todos are [OPE-1448](https://linear.app/openticketai/issue/OPE-1448/desk-core-hub-plugins-persons-privilege-tree). The picture is [`otai-desk-persons`](./diagrams/desk/otai-desk-persons.d2). LD-100 is proposed. LD-097 still forbids a plugin marketplace until LD-100 is accepted.

## Core

A new install is the ticket system.

- Persons, organizations, and memberships
- Queues
- Tickets, articles, and custom attributes on tickets
- Roles, the privilege tree, queue grants, and conditions
- One Razor interface. Pages the person cannot use are absent
- Built-in designs `light-forrest` and `dark-forrest`
- A Plugins screen that installs and uninstalls the modules shipped with Desk

## Plugins

Install and uninstall are part of the first version. The Open ITSM Hub catalog is the marketplace, and it comes after that. Until then the Plugins screen lists the modules that ship with Desk.

- An admin installs and uninstalls FAQ and CMDB from that screen
- Uninstall disables the module and keeps its rows, so a later install shows the same FAQ entries and config items
- A plugin declares the plugins it requires. Install waits until those are installed. Uninstall waits until nothing installed still requires it
- FAQ plugin: FAQ entries, custom attributes on those entries, links from tickets
- CMDB plugin: config item classes. An item has a name, a class, and a number. Every other fact is a custom attribute of that class. Tickets link to items. There is no separate "custom object" feature

## Marketplace, after the first version

- The Plugins screen and the Skins screen list the community packages Open ITSM Hub offers
- A hub skin is a token set. It changes colors only. `light-forrest` and `dark-forrest` stay built in

## Persons

OTOBO splits customer users, customer companies, and a customer interface. Desk keeps one interface.

| Record          | Job                                                                                                         |
| --------------- | ----------------------------------------------------------------------------------------------------------- |
| Person          | The account. Admin, agent, customer, AI agent, and system user are roles on a person.                       |
| Organization    | Company or department the person belongs to.                                                                |
| Membership      | Person in an organization, plus an optional position. The position is shown on a ticket. It grants nothing. |
| Role            | A set of privileges.                                                                                        |
| Role assignment | This person has this role inside this organization.                                                         |

The organization does not grant a privilege. The assignment makes the role apply in that organization only. A person who may see every ticket in one organization still has no access to tickets in another. The same person can be an agent in one organization and a customer in another.

## Privileges

Call the thing being protected the **object**. Call the account the **person**. A privilege names an object or one attribute of that object.

The tree is the inheritance. A grant of a parent includes every child.

```text
ticket
  ticket.view
  ticket.attribute
    ticket.attribute.title
      ticket.attribute.title.view
      ticket.attribute.title.edit
    ticket.attribute.state
      ticket.attribute.state.view
      ticket.attribute.state.edit
    ticket.attribute.<custom>
      view
      edit
  ticket.article
    ticket.article.public
    ticket.article.internal
```

Creating a custom attribute adds its node. FAQ attributes and config-item attributes join the tree when that plugin is installed.

Editing state is how a ticket is closed. Editing queue is how a ticket moves. There is no extra action list beside the attributes.

A **queue grant** is organization + role + queue + privilege. Each queue can carry many privileges. Casbin answers `Enforce(person, organization, queue, privilege)`. A grant of the parent makes the child check succeed.

## Conditions

This is the OTOBO ACL, as rows rather than a second policy language.

Casbin decides whether the privilege is held. A condition then runs:

| Effect   | Result                                                           |
| -------- | ---------------------------------------------------------------- |
| `deny`   | The privilege is refused in this situation.                      |
| `values` | The attribute may only take the listed values.                   |
| `allow`  | The privilege stays allowed when an earlier condition denied it. |

A condition matches on organization and any of: role, queue, ticket state, privilege. Example: in the Incident queue, priority may only be 1 or 2. Casbin cannot return that value list, so the condition stays a Desk table.

## Todos

- [ ] Accept LD-100 and point LD-097 and LD-099 at it
- [ ] Persist person, organization, membership, role, and role assignment as in `otai-desk-persons`
- [ ] Privilege tree: parent grant includes children; custom attributes grow the tree
- [ ] Queue grants and `Enforce(person, organization, queue, privilege)`
- [ ] Condition rows evaluated after Casbin
- [ ] Shell shows only the pages and fields the person may use
- [ ] Plugins screen installs and uninstalls FAQ and CMDB. Uninstall keeps their rows
- [ ] Install and uninstall respect plugin dependencies
- [ ] Later: Plugins and Skins screens list Open ITSM Hub packages
- [ ] FAQ plugin, off until installed, with attributes on FAQ entries
- [ ] CMDB plugin, off until installed, with classes whose items have only custom attributes
- [ ] Ticket link to a FAQ entry or a config item when that plugin is installed
