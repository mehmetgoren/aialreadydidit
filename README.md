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
npm run type-check   # vue-tsc --build
npm run lint         # eslint --fix
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
  i18n/            en-US/ tr-TR/ → common, catalog, dashboard, upload, admin
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
docker/nginx.conf  SPA + /api /files proxy; crawler user-agents on /app/* → API /seo/app/* (server-rendered)
```
