# AI Already Did It — storefront

Vue 3 + TypeScript + [Element Plus](https://element-plus.org) storefront, member dashboard, upload wizard and admin panel for
**AI Already Did It**, the free store of LLM-generated applications. Talks to `../src-backend` (`AiAlreadyDidIt.Api`).

The project mirrors the Gemecik / prototype architecture: boot files, one service class per API controller, a `LocalService`
storage wrapper, Pinia setup stores, flat snake_case i18n keys (en-US default, tr-TR), an admin panel driven by role → menus,
and a multi-stage nginx Docker image that also routes crawlers to the server-rendered app pages.

## Stack

| Concern    | Choice                                                                                          |
|------------|-------------------------------------------------------------------------------------------------|
| Framework  | Vue 3.5 (`<script setup>`), Vite 8, `vue-tsc`                                                    |
| UI         | Element Plus 2.14 (auto-imported on demand, SCSS theme override, dark mode via `html.dark`)       |
| Routing    | Vue Router 5 — guards for auth (`meta.public`) and admin (`meta.requiresAdmin`)                  |
| State      | Pinia setup stores (`user`, `common`, `site`, `category`, `notification`)                        |
| HTTP       | Axios instance (`src/boot/axios.ts`) + `BaseService` classes unwrapping the API envelope; silent access-token refresh through the httpOnly refresh cookie |
| i18n       | vue-i18n 11, flat keys merged from per-domain files (common, catalog, dashboard, upload, admin)  |
| Markdown   | marked + DOMPurify (`MarkdownView`)                                                              |

## Scripts

```bash
npm install
npm run dev          # http://localhost:5174 (API root from .env → http://localhost:5190/api/v1)
npm run build        # type-check + production build (dist/)
npm run type-check   # vue-tsc --build (app + vitest projects)
npm run lint         # eslint --fix
npm run test         # vitest run (unit tests, jsdom)
npm run test:unit    # vitest in watch mode
npm run test:coverage
```

Start the API first (`cd ../src-backend/AiAlreadyDidIt.Api && dotnet run --launch-profile http`), then sign in with a seeded
account, e.g. admin `ioniangamer@gmail.com` / `Aadi123!` or member `demo` / `Aadi123!`.

## Environment

| Variable                | Purpose                                                                                |
|-------------------------|----------------------------------------------------------------------------------------|
| `VITE_API_BASE_URL`     | Absolute API root (`http://localhost:5190/api/v1`) or `/api/v1` behind the nginx proxy. |
| `VITE_API_PROXY_TARGET` | Vite dev proxy target for `/api` and `/files` when the base URL is relative.            |
| `VITE_APP_TITLE`        | Brand shown in `document.title`.                                                        |

Google sign-in appears automatically when the API reports a `googleClientId` (`Site:GoogleClientId`).

## Project layout

```
src/
  boot/            axios (base URL + Bearer + refresh-on-401), i18n, theme, index (installs everything)
  i18n/            locales.ts (registry: code, native label, dayjs id, Intl tag, direction, browser prefixes)
                   en-US/ tr-TR/ es-ES/ pt-BR/ de-DE/ fr-FR/ ar-SA/ ru-RU/ ja-JP/ ko-KR/ zh-CN/ → common, catalog, dashboard, upload, admin
  layouts/         StorefrontLayout (announcement, header, category mega-menu, breadcrumb, footer)
                   DashboardLayout (member left menu from utils/dashboard-menu.ts)
                   AdminLayout (header + drawer LeftMenu fed by /admin/panel/menu)
  components/      layout/ common/ (SavingsCounter, MarkdownView, ScoreBadge, PlatformBadges, StatusTag…)
                   catalog/ (AppCard, FilterSidebar, ScreenshotGallery, DownloadButtons, RatingsSection, PromptBlock, LineageTree…)
                   upload/ (wizard steps: Source, Details, Files, Screenshots, Review) admin/ (AdminPage, AdminDataTable, charts…)
  pages/           catalog/ (Home, Search, Category, App, Uploader, Collection, Wanted, About, ForAgents)
                   account/ dashboard/ (Overview, MyApps, AppEditor = wizard, Downloads, Ratings, Favorites, Collections, Watches, Notifications, ApiKeys, Settings)
                   admin/ (moderation, catalog, members, content, stats, system)
  router/          index (guards) + routes/<feature>.ts + routes/admin/<area>.ts
  stores/          Pinia setup stores
  utils/           models/ (API DTOs), services/ (one per controller), validation/, tools.ts, format.ts, dashboard-menu.ts
  styles/          index.scss (design tokens, helpers), element/index.scss (Element Plus theme)
  **/__tests__/    Vitest specs next to the code they cover (see below)
docker/nginx.conf  SPA + /api /files proxy; crawler user-agents on /app/* → API /seo/app/* (server-rendered)
vitest.config.ts   merges vite.config.ts (mode "test" turns off the Element Plus SCSS imports) + jsdom
```

## Languages

Eleven UI languages ship in `src/i18n/<code>/` (English, Turkish, Spanish, Brazilian Portuguese, German, French, Arabic,
Russian, Japanese, Korean, Simplified Chinese). `src/i18n/locales.ts` is the only registry: it drives the switcher, browser
language detection (`navigator.languages` prefixes, `pt-PT` → `pt-BR`), dayjs and `Intl` formatting, the Element Plus
locale and `<html lang dir>` (Arabic is RTL; only a few directional rules are mirrored, so expect rough edges). English is bundled; every other language is a separate chunk loaded the first time it is selected
(`src/i18n/index.ts` loaders, `loadLocaleMessages`). Adding a language = one registry row, one loader line and one folder
with the same five files. The i18n test enforces that every locale has exactly
the English key set with the same `{placeholders}` and is not a copy of the English text. Category names come from the
database in English and Turkish only, so other languages see the English category names.

## Unit tests

`npm run test` runs the Vitest suites in `src/**/__tests__/*.spec.ts` (jsdom, no API needed). Covered: `format.ts`,
`tools.ts`, validation rules, `BaseService` (envelope unwrapping, URL building, uploads), `LocalService`, the axios boot
(bearer header, single refresh on 401 with retry, sign-out on failed refresh, error normalisation), the user / site /
category / common / notification stores, the router table (public vs. member vs. admin metas, title keys, dashboard menu)
and the navigation guard, `useAppBrowser` (URL ↔ query sync), the `ScoreBadge`, `StatusTag`, `PagePagination`, `MarkdownView`
components, and an i18n parity check (en-US and tr-TR expose identical keys with matching `{placeholders}`, and domain
files never shadow each other). Services are mocked with `vi.mock` class stubs; the axios interceptors are exercised
through a scripted `api.defaults.adapter`.
