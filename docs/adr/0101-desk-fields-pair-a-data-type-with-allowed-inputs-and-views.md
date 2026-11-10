---
id: LD-101
status: "accepted"
date: 2026-11-09
supersedes: []
superseded_by: []
legacy_status: Locked
categories: [product-ux]
---

# Desk fields pair a data type with allowed inputs and views

## Context and Problem Statement

A custom attribute stores one value kind (`string`, `number`, `boolean`, `datetime`, `user`, `select`). The new-ticket screen turns every kind into the same text box, except `select`. A date, a timestamp, and a checkbox are different controls. The same stored value can also be shown in more than one way, and a form needs sections, not one long column.

LD-097 keeps the UI as Razor and htmx. A design changes colors only.

## Decision Drivers

- The control has to match the stored value, and a value is not allowed in every control.
- The same sections, grids, tables, and columns are used on every screen.
- No second frontend, and no widget kit that brings its own markup.

## Considered Options

- Import a component kit (Vue, Lit, or a web-component library) beside Razor.
- Keep one text input and branch on `select` in each page.
- **A catalog in the app names each data type, the inputs it allows, and the views it allows. Razor tag helpers and CSS are the screen components.**

## Decision Outcome

Chosen option: **A catalog in the app names each data type, the inputs it allows, and the views it allows. Razor tag helpers and CSS are the screen components.**

### Binding rules

1. Data types are `string`, `number`, `boolean`, `date`, `datetime`, `user`, and `select`. `datetime` is the timestamp value.
2. Inputs are `text`, `textarea`, `number`, `date`, `timestamp`, `checkbox`, `select`, and `user`. Views are `text`, `prose`, `number`, `date`, `timestamp`, `checkbox`, `badge`, and `user`.
3. `FieldCatalog` is the only list of which inputs and which views a data type allows. A definition stores one input and one view from that list, plus a group name. A pair outside the list fails the save.
4. An input posts one string. The existing value check for the data type still accepts or rejects it. A checkbox that is off posts `false`.
5. Screen structure is the tag helpers `ui-section`, `ui-stack`, `ui-grid`, `ui-columns`, and `ui-table`. `ui-field` renders an input. `ui-value` renders a view. Pages use these helpers. They do not invent a second field or table style.
6. htmx still returns a partial. It does not supply the controls. A design still changes colors only.

## Consequences

- Good: Text, date, select, checkbox, and timestamp are real controls, and a later screen reuses the same helpers.
- Good: Adding a pairing is a catalog change, checked by a test.
- Trade-off: The helpers are ours to maintain. A design cannot swap the markup.

## Links

- Stable id: `LD-101`
- Linear: OPE-1450
- Related: LD-097, LD-098
