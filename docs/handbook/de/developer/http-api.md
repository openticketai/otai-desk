---
title: "HTTP-API"
description: "Diese Routen stehen an den `Map*` Aufrufen unter `src/`."
draft: false
---

# HTTP-API

Diese Routen stehen an den `Map*` Aufrufen unter `src/`.

| Method | Path | Right |
| --- | --- | --- |
| `GET` | `/api/v1/tickets` | `tickets:read` |
| `POST` | `/api/v1/tickets` | `tickets:write` |
| `GET` | `/api/v1/tickets/{id:guid}` | `tickets:read` |
| `GET` | `/api/v1/tickets/{id:guid}/articles` | `tickets:read` |
| `POST` | `/api/v1/tickets/{id:guid}/articles` | `tickets:write` |
| `GET` | `/health` | `public` |
| `POST` | `/api/v1/token` | `public` |
| `GET` | `/api/v1/me` | `signed-in` |

Quelle: `src/`.
