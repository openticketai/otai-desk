import { mkdirSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "playwright";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const catalog = JSON.parse(
  readFileSync(resolve(root, "docs/handbook/catalog.json"), "utf8"),
);
const outDir = resolve(root, "docs/handbook/screenshots");
const base = process.env.OTAI_DESK_URL ?? "http://127.0.0.1:8091";
const email = process.env.OTAI_DESK_DEMO_EMAIL ?? "admin@demo.openticketai.com";
const password = process.env.OTAI_DESK_DEMO_PASSWORD;
if (!password) {
  throw new Error("OTAI_DESK_DEMO_PASSWORD is required");
}

mkdirSync(outDir, { recursive: true });
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });

const login = catalog.screens.find((screen) => screen.slug === "signing-in");
await page.goto(base + login.route, { waitUntil: "networkidle" });
await page.screenshot({ path: resolve(outDir, "signing-in.png") });
await page.fill('input[name="email"]', email);
await page.fill('input[name="password"]', password);
await page.click('button[type="submit"]');
await page.waitForURL((url) => !url.pathname.endsWith("/login"));

for (const screen of catalog.screens) {
  if (screen.slug === "signing-in") {
    continue;
  }
  const route = screen.route.includes("{")
    ? await ticketRoute(page)
    : screen.route;
  await page.goto(base + route, { waitUntil: "networkidle" });
  await page.screenshot({ path: resolve(outDir, `${screen.slug}.png`) });
  console.log(screen.slug, route);
}

await browser.close();

async function ticketRoute(page) {
  await page.goto(base + "/", { waitUntil: "networkidle" });
  const href = await page
    .locator('a[href^="/tickets/"]')
    .first()
    .getAttribute("href");
  if (!href) {
    throw new Error("Demo seed has no ticket to open");
  }
  return href;
}
