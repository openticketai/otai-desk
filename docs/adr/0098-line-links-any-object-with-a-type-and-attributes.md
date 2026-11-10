---
id: LD-098
status: "accepted"
date: 2026-10-30
supersedes: []
superseded_by: []
legacy_status: Locked
categories: [product-ux]
---

# OTAI Desk links any object, with a type and attributes

## Context and Problem Statement

The product name changed from Line to OTAI Desk.

LD-097 put FAQ and configuration items in Line and tied each to tickets with its own link table. OTOBO does not work that way. One link record connects any two objects. The link has a type. The type has a name on each end.

OTAI Desk also needs facts the columns do not cover. Those facts are custom attributes, on the objects and on the link itself.

## Decision Drivers

- The link model matches OTOBO LinkObject.
- A ticket, a FAQ, and a configuration item can link to each other and to their own kind.
- A link is one row. The type supplies the label on each end.
- Custom attributes already exist for tickets (LD-090). FAQ, configuration items, and links use the same definition and value rows.

## Considered Options

- One link table per pair (ticket–FAQ, ticket–configuration item) and no attributes on the link.
- **One link row for every pair of objects. The link has a type. Objects and links carry custom attributes.**

## Decision Outcome

Chosen option: **One link row for every pair of objects. The link has a type. Objects and links carry custom attributes.**

### Binding rules

1. One `Link` row stores `source_kind`, `source_id`, `target_kind`, `target_id`, and `type_id`. Kinds are `ticket`, `faq`, and `configuration_item`. Either end may be any kind, including two of the same kind.
2. A `LinkType` has `name`, `source_label`, and `target_label`. The row is stored once. The source label is the name seen from the source object. The target label is the name seen from the target object.
3. Types that ship: `Normal` (Normal / Normal), `ParentChild` (Parent / Child), `DependsOn` (Depends on / Required by), `RelevantTo` (Relevant to / Relevant from). Further types are rows of the same shape.
4. Do not add a link table per pair.
5. A custom attribute is an `AttributeDefinition` (`name`, `label`, `value_kind`, `required`, `options`) plus an `AttributeValue` (`definition_id`, `owner_kind`, `owner_id`, `value`). `owner_kind` is `ticket`, `faq`, `configuration_item`, or `link`.
6. A definition for `owner_kind` `link` may name one `link_type_id`. Other owners do not.

## Consequences

- Good: FAQ–FAQ, configuration item–configuration item, and ticket–ticket use the same link as ticket–FAQ.
- Good: A link can store its own facts without a new column.
- Trade-off: The ticket–FAQ routes in the older TypeSpec are the pair shape this decision retires.

## Links

- Stable id: `LD-098`
- Related: LD-090, LD-097
- Diagrams: `otai-desk-faq-cmdb`
