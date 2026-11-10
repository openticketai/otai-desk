# Ticket-system auth capabilities (Desk)

Desk does **not** copy groups or policies from the ticket system (LD-085).
The connector proves identity. Desk stamps `DeskRole` from `as_role`.

Deploy: `OTAI_DESK_PORTAL_MODE` = `both` | `customer_only` | `agent_only`.
`GET /portal` returns mode ∩ these capabilities.

| Provider            | `customer_auth` | `agent_auth` | Notes                                                                 |
| ------------------- | --------------- | ------------ | --------------------------------------------------------------------- |
| KIX                 | yes             | yes          | `authenticate_customer` implemented (`UserType: Customer`)            |
| OTOBO               | no (today)      | yes          | GI raises `UnsupportedTicketSystemCapabilityError` for customer login |
| Znuny               | no (today)      | yes          | same GI path as OTOBO                                                 |
| Zammad              | no (today)      | yes          | customer login not implemented                                        |
| OTAI Ticket Backend | later           | later        | fifth connector target; not Desk-as-SoR                               |

If `customer_auth` is no, or mode is `agent_only`, Desk shows **Lite Agent**
only. Do not render a customer login that cannot succeed.

When a provider adds customer login, flip the cell here and the connector
adapter. Do not invent Desk-side group mapping.
