#!/usr/bin/env python3
"""Generate the OTAI Desk handbook from the Razor screens and the HTTP API.

German and English pages are written from the same screen catalog. A screenshot
is embedded when docs/handbook/screenshots/<slug>.png exists.

    python otai-desk/scripts/handbook.py
    python otai-desk/scripts/handbook.py publish
"""

from __future__ import annotations

import json
import re
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src"
HANDBOOK = ROOT / "docs" / "handbook"
GITHUB_URL = "https://github.com/openticketai/otai-desk"
SHOTS = HANDBOOK / "screenshots"
SITE_CONTENT = ROOT / "docs-site" / "src" / "content" / "docs"

RIGHTS = {
    "TicketsRead": "tickets:read",
    "TicketsWrite": "tickets:write",
    "AttributesManage": "attributes:manage",
    "UsersManage": "users:manage",
}
ADMIN_RIGHTS = {"attributes:manage", "users:manage"}

# UI headings are English. German is the parallel title, not a second product name.
TITLES_DE = {
    "Sign in": "Anmelden",
    "Tickets": "Tickets",
    "New ticket": "Neues Ticket",
    "Knowledge": "Wissen",
    "Design": "Design",
    "Users": "Benutzer",
    "Accounts": "Konten",
    "Attributes": "Attribute",
    "Ticket": "Ticket",
}

SKIP_ROUTES = {"/logout"}
FALLBACK_TITLES = {"ticket": "Ticket"}


def main() -> None:
    command = sys.argv[1] if len(sys.argv) > 1 else "generate"
    if command == "generate":
        generate()
    elif command == "publish":
        generate()
        publish()
    else:
        raise SystemExit(f"Unknown command: {command}")


def generate() -> None:
    screens = screen_catalog()
    routes = api_catalog()
    settings = settings_catalog()
    for locale in ("de", "en"):
        locale_dir = HANDBOOK / locale
        if locale_dir.exists():
            shutil.rmtree(locale_dir)
    HANDBOOK.mkdir(parents=True, exist_ok=True)
    (HANDBOOK / "catalog.json").write_text(
        json.dumps(
            {"screens": screens, "routes": routes, "settings": settings}, indent=2
        )
        + "\n",
        encoding="utf-8",
    )
    write_indexes(screens, routes)
    for screen in screens:
        write_screen(screen)
    write_running(settings)
    write_api(routes)
    print(f"handbook: {len(screens)} screens, {len(routes)} routes -> {HANDBOOK}")


def publish() -> None:
    if SITE_CONTENT.exists():
        shutil.rmtree(SITE_CONTENT)
    shutil.copytree(
        HANDBOOK, SITE_CONTENT, ignore=shutil.ignore_patterns("catalog.json")
    )
    print(f"published -> {SITE_CONTENT}")


def screen_catalog() -> list[dict[str, object]]:
    models = page_models()
    screens: list[dict[str, object]] = []
    for path in sorted(SRC.rglob("*.cshtml")):
        if "tests" in path.parts or path.name.startswith("_"):
            continue
        text = path.read_text(encoding="utf-8")
        route_match = re.search(r'@page\s+"([^"]+)"', text)
        if route_match is None:
            continue
        route = route_match.group(1)
        if route in SKIP_ROUTES:
            continue
        model_match = re.search(r"@model\s+([\w.]+)", text)
        model_name = model_match.group(1).rsplit(".", 1)[-1] if model_match else ""
        meta = models.get(
            model_name, {"right": None, "anonymous": False, "handlers": []}
        )
        slug = slug_for(route)
        title = first_heading(text) or FALLBACK_TITLES.get(slug, route)
        markup = text + partial_markup(path, text)
        right = meta["right"]
        screens.append(
            {
                "slug": slug,
                "route": route,
                "title": title,
                "title_de": TITLES_DE.get(title, title),
                "audience": audience_for(
                    bool(meta["anonymous"]), right if isinstance(right, str) else None
                ),
                "right": right,
                "anonymous": meta["anonymous"],
                "handlers": meta["handlers"],
                "fields": unique(
                    [
                        name
                        for name in re.findall(
                            r'<label class="field">\s*([^<]+?)\s*(?:<|$)', markup
                        )
                        + re.findall(r'<ui-field\b[^>]*\blabel="([^"]+)"', markup)
                        if not name.startswith("@")
                    ]
                ),
                "columns": unique(re.findall(r"<th>([^<]+)</th>", markup)),
                "actions": unique(
                    re.findall(r"<button\b[^>]*>\s*([^<]+?)\s*</button>", markup)
                ),
                "source": path.relative_to(ROOT).as_posix(),
            }
        )
    screens.sort(key=lambda item: (str(item["audience"]), str(item["slug"])))
    return screens


def page_models() -> dict[str, dict[str, object]]:
    found: dict[str, dict[str, object]] = {}
    pattern = re.compile(
        r"((?:\[[^\]]+\]\s*)+)public sealed class (\w+)",
        re.MULTILINE,
    )
    for path in SRC.rglob("*.cs"):
        if "tests" in path.parts:
            continue
        text = path.read_text(encoding="utf-8")
        matches = list(pattern.finditer(text))
        for index, match in enumerate(matches):
            attrs = match.group(1)
            name = match.group(2)
            end = matches[index + 1].start() if index + 1 < len(matches) else len(text)
            body = text[match.end() : end]
            policy = re.search(r"Authorize\(Policy = Rights\.(\w+)\)", attrs)
            found[name] = {
                "anonymous": "AllowAnonymous" in attrs,
                "right": RIGHTS[policy.group(1)] if policy else None,
                "handlers": unique(re.findall(r"\b(On(?:Get|Post)\w*)\b", body)),
            }
    return found


def partial_markup(page: Path, text: str) -> str:
    extra: list[str] = []
    for name in re.findall(r'partial name="([^"]+)"', text):
        for candidate in (
            page.parent / f"{name}.cshtml",
            SRC / "Pages" / "Shared" / f"{name}.cshtml",
        ):
            if candidate.is_file():
                extra.append(candidate.read_text(encoding="utf-8"))
                break
    return "\n".join(extra)


def api_catalog() -> list[dict[str, str]]:
    routes: list[dict[str, str]] = []
    for path in sorted(SRC.rglob("*.cs")):
        if "tests" in path.parts:
            continue
        text = path.read_text(encoding="utf-8")
        for match in re.finditer(
            r"(app|api)\.Map(Get|Post|Put|Delete)\(\"([^\"]+)\"", text
        ):
            owner, method, route = match.group(1), match.group(2), match.group(3)
            window = text[match.end() :]
            nxt = re.search(r"\n\s*(?:app|api)\.Map", window)
            if nxt:
                window = window[: nxt.start()]
            policy = re.search(r"RequireAuthorization\(Rights\.(\w+)\)", window)
            signed_in = "RequireAuthorization()" in window
            if owner == "api":
                route = "/api/v1" + route
            right = (
                RIGHTS[policy.group(1)]
                if policy
                else ("signed-in" if signed_in else "public")
            )
            routes.append(
                {
                    "method": method.upper(),
                    "path": route,
                    "right": right,
                    "source": path.relative_to(ROOT).as_posix(),
                }
            )
    return routes


def settings_catalog() -> list[str]:
    text = (SRC / "OtaiDeskApp.cs").read_text(encoding="utf-8")
    names = re.findall(r'(?:Required|GetEnvironmentVariable)\("([^"]+)"\)', text)
    return unique(names)


def write_indexes(
    screens: list[dict[str, object]], routes: list[dict[str, str]]
) -> None:
    groups = {
        "user": ("Benutzer", "User"),
        "administrator": ("Administrator", "Administrator"),
        "developer": ("Entwickler", "Developer"),
    }
    for locale, title_key, intro in (
        (
            "de",
            "title_de",
            "Diese Seiten erzeugt `otai-desk/scripts/handbook.py` aus den Razor-Seiten und der HTTP-API.",
        ),
        (
            "en",
            "title",
            "These pages are generated by `otai-desk/scripts/handbook.py` from the Razor screens and the HTTP API.",
        ),
    ):
        source = (
            f"Quellcode auf [GitHub]({GITHUB_URL})."
            if locale == "de"
            else f"Source code on [GitHub]({GITHUB_URL})."
        )
        lines = [
            frontmatter("OTAI Desk", intro),
            f"# OTAI Desk\n\n{intro}\n\n{source}\n",
        ]
        for audience, (de_name, en_name) in groups.items():
            name = de_name if locale == "de" else en_name
            lines.append(f"\n## {name}\n")
            for screen in screens:
                if screen["audience"] != audience:
                    continue
                label = screen[title_key]
                lines.append(f"- [{label}]({audience}/{screen['slug']}/)")
            if audience == "developer":
                running = "Betrieb" if locale == "de" else "Running"
                api = "HTTP-API"
                lines.append(f"- [{running}](developer/running/)")
                lines.append(f"- [{api}](developer/http-api/)")
        write_locale(locale, "index.md", "\n".join(lines) + "\n")
        for audience, (de_name, en_name) in groups.items():
            name = de_name if locale == "de" else en_name
            body = frontmatter(name, intro) + f"# {name}\n\n{intro}\n"
            write_locale(locale, f"{audience}/index.md", body)


def write_screen(screen: dict[str, object]) -> None:
    slug = str(screen["slug"])
    shot = SHOTS / f"{slug}.png"
    image = (
        f"\n![{screen['title']}](../../screenshots/{slug}.png)\n"
        if shot.is_file()
        else ""
    )
    right = screen["right"]
    for locale in ("de", "en"):
        title = str(screen["title_de"] if locale == "de" else screen["title"])
        if locale == "de":
            access = (
                "Ohne Anmeldung."
                if screen["anonymous"]
                else (f"Recht: `{right}`." if right else "Angemeldete Personen.")
            )
            prose = f"**{title}** öffnen Sie unter `{screen['route']}`. {access}"
            fields_h, actions_h, columns_h = "Felder", "Aktionen", "Spalten"
            source_h = "Quelle"
        else:
            access = (
                "No sign-in."
                if screen["anonymous"]
                else (f"Right: `{right}`." if right else "Signed-in people.")
            )
            prose = f"Open **{title}** at `{screen['route']}`. {access}"
            fields_h, actions_h, columns_h = "Fields", "Actions", "Columns"
            source_h = "Source"
        parts = [frontmatter(title, prose), f"# {title}\n\n{prose}\n", image]
        parts.append(bullet_section(fields_h, screen["fields"]))
        parts.append(bullet_section(columns_h, screen["columns"]))
        parts.append(bullet_section(actions_h, screen["actions"]))
        handlers = screen["handlers"]
        if isinstance(handlers, list) and handlers:
            parts.append(
                bullet_section(
                    "Handler" if locale == "de" else "Handlers",
                    [f"`{name}`" for name in handlers],
                )
            )
        parts.append(f"\n## {source_h}\n\n`{screen['source']}`\n")
        write_locale(locale, f"{screen['audience']}/{slug}.md", "".join(parts))


def write_running(settings: list[str]) -> None:
    for locale, title, intro in (
        (
            "de",
            "Betrieb",
            "Diese Variablen liest die App beim Start. Eine fehlende Pflichtvariable bricht den Start ab.",
        ),
        (
            "en",
            "Running",
            "The app reads these variables at startup. A missing required variable stops the process.",
        ),
    ):
        rows = "\n".join(f"- `{name}`" for name in settings)
        source = "Quelle" if locale == "de" else "Source"
        body = (
            frontmatter(title, intro)
            + f"# {title}\n\n{intro}\n\n{rows}\n\n{source}: `src/OtaiDeskApp.cs`.\n"
        )
        write_locale(locale, "developer/running.md", body)


def write_api(routes: list[dict[str, str]]) -> None:
    rows = "\n".join(
        f"| `{route['method']}` | `{route['path']}` | `{route['right']}` |"
        for route in routes
    )
    for locale, title, intro, source in (
        (
            "de",
            "HTTP-API",
            "Diese Routen stehen an den `Map*` Aufrufen unter `src/`.",
            "Quelle: `src/`.",
        ),
        (
            "en",
            "HTTP API",
            "These routes are the `Map*` calls under `src/`.",
            "Source: `src/`.",
        ),
    ):
        body = (
            frontmatter(title, intro)
            + f"# {title}\n\n{intro}\n\n| Method | Path | Right |\n| --- | --- | --- |\n{rows}\n\n{source}\n"
        )
        write_locale(locale, "developer/http-api.md", body)


def write_locale(locale: str, relative: str, body: str) -> None:
    path = HANDBOOK / locale / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(body, encoding="utf-8")


def frontmatter(title: str, description: str) -> str:
    safe = description.replace('"', "'")
    return f'---\ntitle: "{title}"\ndescription: "{safe}"\ndraft: false\n---\n\n'


def bullet_section(heading: str, items: object) -> str:
    if not isinstance(items, list) or not items:
        return ""
    lines = "\n".join(f"- {item}" for item in items)
    return f"\n## {heading}\n\n{lines}\n"


def first_heading(text: str) -> str | None:
    match = re.search(r"<h1\b[^>]*>\s*([^<]+?)\s*</h1>", text)
    return match.group(1) if match else None


def slug_for(route: str) -> str:
    if route == "/":
        return "tickets"
    if route == "/login":
        return "signing-in"
    if route == "/tickets/new":
        return "new-ticket"
    if route.startswith("/tickets/"):
        return "ticket"
    return route.strip("/").replace("/", "-")


def audience_for(anonymous: bool, right: str | None) -> str:
    if anonymous:
        return "user"
    if right in ADMIN_RIGHTS:
        return "administrator"
    return "user"


def unique(items: list[str]) -> list[str]:
    seen: list[str] = []
    for item in items:
        clean = " ".join(item.split())
        if clean and clean not in seen:
            seen.append(clean)
    return seen


if __name__ == "__main__":
    main()
