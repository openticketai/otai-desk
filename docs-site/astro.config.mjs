import starlight from "@astrojs/starlight";
import { defineConfig } from "astro/config";

export default defineConfig({
  site: "https://desk.openticketai.com",
  base: "/docs",
  trailingSlash: "always",
  integrations: [
    starlight({
      title: "OTAI Desk",
      social: [
        {
          icon: "github",
          label: "GitHub",
          href: "https://github.com/openticketai/otai-desk",
        },
      ],
      defaultLocale: "de",
      locales: {
        de: { label: "Deutsch", lang: "de" },
        en: { label: "English", lang: "en" },
      },
      sidebar: [
        {
          label: "Benutzer",
          translations: { en: "User" },
          autogenerate: { directory: "user" },
        },
        {
          label: "Administrator",
          translations: { en: "Administrator" },
          autogenerate: { directory: "administrator" },
        },
        {
          label: "Entwickler",
          translations: { en: "Developer" },
          autogenerate: { directory: "developer" },
        },
      ],
    }),
  ],
});
