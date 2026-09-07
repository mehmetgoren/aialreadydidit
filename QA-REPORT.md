# QA report — 2026-09-06 (pre-production pass)

Tested in Chrome (Claude Chrome extension, owner's admin session) plus headless Chrome for anonymous pages, against the
docker-compose stack (web 9002, api 5190). Backend 110 + frontend 103 unit tests green after the fixes.

## What was exercised and works
- Storefront: home, category bar + More menu, search (hybrid / keyword / semantic, empty state), category page + facets,
  app page (gallery, download box, savings, source panel, Description / Prompts / Versions / Reviews tabs, similar apps),
  wanted board, for-agents page, about, 404, login / signup / forgot-password (anonymous), EN ↔ TR switch.
- Member: dashboard overview, my apps, downloads, ratings, favorites, collections, API keys, settings; notification bell;
  user menu.
- Publishing wizard end to end: source archive upload + analysis (license detected), details (manual category /
  sub-category picker, license, model, prompt), install file (wrong-platform upload correctly rejected with 422),
  screenshot upload, readiness checklist, submit → ClamAV scan → moderation queue → review page → approve → published,
  notification sent, embedding job, app found by hybrid search. Test app removed afterwards.
- Admin: dashboard, moderation queue, review, reports, apps, categories, licenses, platforms, users, sessions, settings,
  stats, jobs, health (no secrets in the effective configuration), audit log. No console errors on any page.
- HTTP: robots.txt, sitemap.xml, llms.txt, API 404 envelope, crawler 404 for unknown slugs, CORS closed for foreign origins,
  X-Forwarded-For honoured behind nginx.

## Fixed in this pass
| # | Finding | Fix |
|---|---|---|
| 1 | After a deploy an open tab could request chunk hashes that no longer exist → blank page | `main.ts` listens for `vite:preloadError` and reloads once |
| 2 | nginx sent no security headers | `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy` |
| 3 | Production anonymous rate limit 60 req/min per IP (a browsing session exceeds it; offices share IPs) | 240 anonymous / 600 member per minute |
| 4 | Keyword search had no stemming (`hardware monitor` found nothing although "Hardware monitoring" exists) | tsvector = english-stemmed ∥ exact tokens, query with `english`; migration `SearchVectorEnglishStemming` |
| 5 | Nonsense query returned CPU-Z at 36 % similarity | `search.min_similarity` default 0.35 → 0.45 (also updated on this DB) |
| 6 | Every visit to "Publish an app" created a new "Untitled app" draft | the wizard reuses an existing empty draft |
| 7 | README images with relative paths were broken | rewritten to raw.githubusercontent / gitlab raw for repo apps; still-broken images hidden |
| 8 | README table first column wrapped letter by letter ("CP U") | first column `white-space: nowrap` |
| 9 | Search results header said "0 apps" while loading | count hidden until loaded |
| 10 | Header search placeholder truncated | "Search apps…" / "Uygulama ara…" |
| 11 | "1 ratings" | plural forms |
| 12 | Dashboard card "downloads" ambiguous next to "My downloads" | "downloads of my apps" |
| 13 | Element Plus deprecation warning (`el-pagination small`) | `size="small"` |
| 14 | Session IPs shown as `::ffff:172.19.0.1` | IPv4-mapped addresses normalised |
| 15 | Success toasts covered the sticky header | toast offset 72 px |
| 16 | Wizard breadcrumb kept "Untitled app" after renaming | breadcrumb/title refresh on save |
| 17 | Filterable selects ignored Enter on the first match | `default-first-option` |
| 18 | API-keys table clipped its last header | tighter column widths |

## Still open — decide before going public
- **Do not publish API port 5190 (and think about 5191) directly**: the forwarded-headers middleware trusts any proxy, so a
  client hitting the API port directly can spoof `X-Forwarded-For` and dodge per-IP rate limits. Keep both behind
  nginx/TLS or bind them to 127.0.0.1 in the production compose file.
- ~~Content-Security-Policy~~ — added 2026-09-08 (`src-frontend/docker/security-headers.conf`, `style-src 'unsafe-inline'`
  because Element Plus sets inline styles; scripts/frames/connect only allow self + Google Identity Services).
- ~~Install files validated by extension only~~ — magic-byte checks added 2026-09-08 (`InstallerSignature`): renamed web
  pages / text files are rejected for every binary format; `.dmg` and `.flatpak` (no stable header) get the web-page check only.
- Review page shows the app and version status twice when equal ("In review · In review").
- Stale-shell caching: tabs opened *before* the `Cache-Control: no-cache` fix may still hold an old `index.html` for a
  while (per URL); harmless after the first production deploy.
- Real content: the seed prompts are reconstructions; Google client id, SMTP and a real `JWT_KEY` are still placeholders.
