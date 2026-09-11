# CLAUDE.md — AI Already Did It

Project guide and session log for Claude Code. Read this first; it records what the project is, every decision taken,
what was built, how to run and verify it, the problems that were hit and how they were fixed, and what is still open.
Detailed plan: `ARCHITECTURE.md`. Backend details: `src-backend/README.md`. Frontend details: `src-frontend/README.md`.

---

## 1. The brief (from the owner, 2026-09-05)

Build **"AI Already Did It"**, a web application: a repository / free marketplace for applications written by LLMs.
Humans and LLM agents search previously LLM-generated apps by keyword or semantically and download them freely.

Purpose:
1. Prevent the same applications from being generated over and over by LLMs (wasted compute and energy).
2. Save users from burning their own tokens and money on apps that already exist.

Three golden rules for every uploaded app (enforced):
1. Open source — mandatory SPDX license field plus a check that the repository/archive contains a LICENSE file matching it.
2. Installable on specific, declared platforms.
3. Free.

Mandatory upload requirements (validated on client and server): source code (public GitHub/GitLab URL — primary path,
imports README, LICENSE, language, stars, releases — or a source archive as fallback); at least one ready-to-run install
file per declared platform (.exe/.msi/.deb/.rpm/.dmg/.apk/.AppImage/Docker image ref/web bundle); at least one screenshot
(minimum enforced, 3+ recommended).

Metadata per app: hierarchical category, name, short + long description, tags; platforms (Windows/Linux/macOS/Web/
Android/iOS/Docker/CLI) each with its own install file; versions with files, changelog, release date; generating LLM
(model + version) and the original prompt(s); optional lineage ("derived from / fork of"); SPDX license.

Other requirements: live semantic duplicate detection while typing ("This looks ~85% similar to X — contribute or fork?");
public REST API + MCP server (`search_apps`, `get_app`, `list_categories`, `download_app`) with API keys and rate
limiting; storefront (description, screenshots, rating, rating count, downloads, platforms, license, model);
sahibinden.com-style category-first search refined by platform/license/model/rating/tags, keyword + semantic combined;
login/signup with e-mail and Google (registration optional, anonymous search/download); ratings out of 100 + review +
"worked / didn't work on version X", only for members who downloaded; user dashboard (my apps, ratings, downloads,
favorites/collections, new-version notifications, API keys); report abuse; homepage savings counter; TR/EN i18n; SEO
(app pages server-rendered or prerendered); a very comprehensive admin panel (moderation queue with approval, reports,
users, categories/tags, license/platform lists, featured, statistics, audit log, system health); ClamAV scanning; JWT +
refresh tokens; PostgreSQL; pgvector preferred over OpenSearch unless justified; MinIO; ASP.NET Core 10 Web API in
`src-backend`; Vue 3 + TypeScript + Element Plus in `src-frontend`; docker-compose for the whole stack.

References: mirror the structure/template of `/mnt/sdy1/products/Gemecik` (`src-backend`, `src-frontend`) exactly;
seed the store with the two LLM-generated projects in `/mnt/sdy1/stuff/LLM generated projects` (`cpu_z`, `hw_monitor`);
borrow UI ideas from Google Play / App Store; overall model is sahibinden.com but everything is free. Missing features
may be added. Produce a plan and ask blocking questions before writing code.

Earlier Turkish notes in `prompts.txt` add: "Kategorilere ayır. Bunu yaparken de LLM karar versin. Gerekirse yeni bir
kategori oluştursun" — the LLM should pick the category and may propose a new one.

## 2. Blocking questions and the owner's answers

| Question | Answer |
|---|---|
| Embedding provider | Ollama (local container running qwen3.8 — the owner's local model) **and** an OpenAI-compatible API key in production. One interface, derived classes, selection from appsettings. |
| LLM-assisted categorization | Yes, include it; use the same abstraction. Pattern reference: the owner's CL-AI project (`LlmProvider` enum + `LlmFactory` creating chat model and embeddings per provider, per-provider config sections). |
| cpu_z has no LICENSE | Add an MIT LICENSE to CPU-Z. Rule: a user uploads source code **or** a GitHub/GitLab URL, never both. |
| Google OAuth | Build the flow behind a config value (button hidden until `Site:GoogleClientId` is set). |
| Default language | English. |
| Admin account | the owner's e-mail (set via `ADMIN_EMAIL`, not stored in the repo). |

Note: the CL-AI settings files contain real API keys; they were read for the pattern only and must never be copied here.

## 3. Decisions

- **Search: PostgreSQL + pgvector + tsvector, no OpenSearch.** Corpus is small and short, decisive filters are relational,
  and one database keeps moderation status transactional. Hybrid = reciprocal rank fusion of keyword rank (weighted
  generated tsvector, GIN + trigram on name) and vector rank (HNSW cosine, 1024-d). Same vector query powers the duplicate
  warning and the agent verdict. `ISearchIndex`-style swap to OpenSearch is possible later but not justified.
- **Embeddings**: `bge-m3` (multilingual, Turkish works) at 1024 dims; OpenAI `text-embedding-3-*` is requested with
  `dimensions=1024` so the column width never changes. Chat: `qwen3.8:latest` locally, `gpt-5-mini` default for OpenAI.
- **AI abstraction** (`Infrastructure/Ai`): `ILlmProvider` (embeddings + chat + health), `OllamaLlmProvider`,
  `OpenAiCompatibleLlmProvider`, `NullLlmProvider`; `LlmProviderFactory` picks by `Ai:Provider` with optional
  `Ai:EmbeddingProvider` / `Ai:ChatProvider` overrides (empty string = inherit).
- **Similarity thresholds**: duplicate/download verdict 0.72 (bge-m3 scale; admin setting `search.duplicate_threshold`),
  fork verdict from threshold − 0.22, semantic floor 0.35.
- **Source rule**: repository OR archive. Attaching a repo deletes an uploaded archive and vice versa.
- **Downloads** go through `GET /api/v1/apps/{slug}/download/{fileId}` which records the download (rating gate + savings)
  and 302-redirects to a presigned MinIO URL (`?json=1` returns the link, sha256 and install hint for agents).
  Screenshots/icons/banners are streamed through `/files/{bucket}/{key}` with long cache headers.
- **Auth**: PBKDF2 passwords; JWT access token 15 min; rotating refresh token 30 days in an httpOnly cookie scoped to
  `/api/v1/account`; the SPA silently refreshes on 401 and retries. Google via ID-token verification. API keys
  `aad_…` (hashed) via `X-Api-Key` or Bearer; a policy scheme picks JWT or ApiKey per request; keys never get admin rights.
- **Rate limiting**: fixed window per API key tier / member / IP, plus stricter `downloads` and `auth` policies.
- **Moderation**: submit → PendingScan (ClamAV job) → PendingReview → admin approve → Published. Trusted uploaders
  (trust level ≥ 1) publish new versions without review (setting). Infected files auto-reject and create a report.
- **SEO** without changing the Vue template: nginx routes crawler user-agents on `/app/{slug}` to the API's
  server-rendered `/seo/app/{slug}` (meta, Open Graph, JSON-LD SoftwareApplication); `/sitemap.xml`, `/robots.txt`,
  `/llms.txt` come from the API. Named regex capture (`(?<slug>…)`) is required because the `map` regex resets `$1`.
- **Savings counter**: tokens = source lines × tokensPerLine (12) × iterationFactor (3), uploader may override with real
  numbers; cost/kWh/CO₂ from admin coefficients; total = Σ downloads × per-app tokens (+ base tokens setting).
- **LLM categorisation is opt-in** (2026-09-06, owner: it needs a GPU server or paid API): `Ai:EnableCategorySuggestions`
  defaults to **false** (`AI_CATEGORY_SUGGESTIONS` in `.env`). Off → the wizard shows explicit Category / Sub-category /
  optional Type selects (`CategoryPicker.vue`), readiness blocks submission unless a non-"Other" category of level ≥ 2 is
  chosen, and the `categorize_app` job is not enqueued. On → previous behaviour (cascader + "Suggest" button, background
  suggestion shown to moderators). `GET site/config` → `categorySuggestionsAvailable` drives the UI.
- **Extra features added**: "wanted" request board (fed by agents too), developer replies and helpful votes on reviews,
  public collections, trending/newest/updated/top-rated rows, lineage tree, "make my own variant" prompt copier, search
  analytics with zero-result queries, background-job monitor, system health page, llms.txt, agent playground page.
- **Install files are sniffed, not just extension-checked** (2026-09-08): `Infrastructure/Import/InstallerSignature` reads the
  first 32 KiB of every uploaded / imported installer and matches the magic bytes the extension promises (MZ, OLE/MSI, ar/.deb,
  RPM, ELF or ISO 9660 for AppImage, squashfs, xar/.pkg, zip family incl. apk/aab/ipa/jar/whl/msix, gzip/xz/ustar, text for
  scripts). Renamed web pages/text files are rejected with a message naming the expected format; `.dmg`/`.flatpak` have no
  stable header and only get the web-page check.
- **Content-Security-Policy** (2026-09-08) lives in `src-frontend/docker/security-headers.conf`, included by every nginx
  location (nginx drops inherited `add_header`s once a location adds its own). `style-src 'unsafe-inline'` stays because
  Element Plus sets inline styles; scripts/frames/connect allow only self + `accounts.google.com`; `img-src https:` for README
  images of linked repositories. The Scalar docs route gets baseline headers only (it loads its UI from a CDN).
- **ImageSharp pinned to 3.1.x** — version 4 fails the Docker build without a paid Six Labors license key.
- **Ports** avoid Gemecik (5182/9001): web 9002, api 5190, mcp 5191, db 5433, minio 9000/9090, ollama 11435 (container).
- Default locale English; Gemecik's Turkish error strings were replaced with English ones.
- **11 UI languages** (2026-09-06, owner): en-US, tr-TR, es-ES, pt-BR, de-DE, fr-FR, ar-SA (RTL), ru-RU, ja-JP, ko-KR, zh-CN.
  Registry `src-frontend/src/i18n/locales.ts`; translations were produced per language by parallel LLM agents from the
  English files and are enforced by the parity test (same keys, same placeholders, < 35 % identical values). Backend
  `AccountService.NormalizeLocale` maps any BCP 47 tag to the nearest supported one. Category names stay EN/TR in the DB.

## 4. Layout

```
AiAlreadyDidIt/
  ARCHITECTURE.md              plan + data model
  CLAUDE.md                    this file
  README.md                    run instructions
  docker-compose.yml           db (pgvector/pg18) · minio · clamav · ollama · api · mcp · web
  .env.example / .env          compose configuration (.env is gitignored; on this machine it points at host Ollama)
  docker/clamav/clamd.conf     StreamMaxLength 1024M etc.   docker/ollama/entrypoint.sh   pulls OLLAMA_PULL_MODELS
  seed-projects/               fallback mount for LLM_PROJECTS_PATH
  src-backend/                 (mirrors Gemecik/src-backend; own git repo, initial commit 4917732)
    AiAlreadyDidIt.slnx
    AiAlreadyDidIt.Api/        Contracts/ Controllers/V1/ Data/{Configurations,Migrations,Seed} Entities/ Infrastructure/ Services/
    AiAlreadyDidIt.Mcp/        StoreTools.cs (7 tools + reuse_first prompt), AadiApiClient.cs, Program.cs (http | --stdio)
    AiAlreadyDidIt.Tests/      xunit unit tests (110, pure logic, no DB/network) — `dotnet test` in src-backend
  src-frontend/                (mirrors Gemecik/src-frontend; own git repo, initial commit 71bf4d3)
    src/{boot,i18n/{en-US,tr-TR},layouts,components,pages,router,stores,utils,styles}
    src/**/__tests__/          Vitest specs (96, jsdom, no API) — `npm run test` in src-frontend
    docker/nginx.conf          SPA + /api,/files proxy (HTTP/1.1) + crawler → /seo/app/*
```

Backend conventions carried from Gemecik: Farmazon-style envelope `{statusCode, statusMessage, result, errors}`,
`ApiException` → middleware, snake_case DB naming, `[Authorize] + [RoleActionAuthorize]` on admin controllers with
`roles/menus/role_menus/role_actions` tables, services auto-registered by the `*Service` suffix (job handlers by `*Job`),
`Data/Seed/ReferenceData.cs` (idempotent reference lists, seeded at startup) and `Data/DbSeeder.cs` (Development demo).

Frontend conventions carried from Gemecik: boot files, one `BaseService` subclass per controller, `LocalService`,
Pinia setup stores, flat snake_case i18n keys merged from per-domain files, `AdminPage`/`AdminDataTable`/`LeftMenu`,
three layouts (Storefront, Dashboard, Admin). Admin menu comes from `GET /admin/panel/menu` (role-filtered), with
`pages/admin/admin-menu.ts` as fallback.

## 5. Key endpoints

| Area | Endpoints |
|---|---|
| Account | `POST account/signup·signin·google·refresh·signout`, `GET account/me`, profile/password/verify/reset, sessions |
| Site | `GET site/config` (boot config, lists, limits, feature flags), `GET site/savings` |
| Catalog | `GET catalog/categories·platforms·licenses·llm-models·tags·home`, `GET catalog/apps` (search + facets), `GET catalog/apps/{slug}` (+ `/similar`, `/lineage`), uploaders, public collections |
| Apps | `GET apps/{slug}/download/{fileId}`, ratings CRUD/vote/reply, `POST apps/{slug}/report` |
| My apps (wizard) | `api/v1/my/apps` — drafts, `inspect-repository`, `source/repository`, `source/archive`, versions, files (upload / external reference / import release asset), screenshots, icon, `check-duplicates`, `suggest-metadata`, submit/withdraw/unlist |
| Dashboard | `api/v1/my/` overview, downloads, ratings, favorites, collections, watches, notifications, api-keys |
| Requests | `api/v1/requests` (wanted board) |
| Agent | `GET agent` (docs), `GET agent/check?q=&platform=&take=` → download / fork / build |
| SEO | `/seo/app/{slug}`, `/sitemap.xml`, `/robots.txt`, `/llms.txt` |
| Admin | `admin/panel`, `admin/moderation`, `admin/reports`, `admin/requests`, `admin/apps`, `admin/categories`, `admin/tags`, `admin/licenses`, `admin/platforms`, `admin/llm-models`, `admin/users`, `admin/api-keys`, `admin/sessions`, `admin/identity` (roles/menus/role-menus/role-actions), `admin/featured`, `admin/banners`, `admin/stats` (+ `/search`, `/savings`), `admin/settings`, `admin/audit-log`, `admin/jobs` (+ maintenance), `admin/health` |

MCP tools: `check_before_building`, `search_apps`, `get_app`, `list_categories`, `download_app`, `submit_app_request`,
`get_savings`. API keys may be forwarded to the MCP server as `X-Api-Key` header or set as `Api:Key`.

## 6. Running

Everything: `cp .env.example .env` → `docker compose up --build` → http://localhost:9002 (API 5190, MCP 5191, MinIO
console 9090). First start migrates, seeds reference data + admin, creates buckets, ClamAV downloads signatures, Ollama
pulls `OLLAMA_PULL_MODELS`; in Development the two demo apps are published and a `demo` member is created.

Without Docker (what was used during development):
```bash
docker start aadi-dev-db aadi-dev-minio aadi-dev-clamav     # pgvector on 5433, MinIO 9000/9090, clamd 3310
ollama pull bge-m3 && ollama pull qwen3.8                    # host Ollama on 11434
cd src-backend/AiAlreadyDidIt.Api && dotnet run --launch-profile http    # 5190
cd src-backend/AiAlreadyDidIt.Mcp && dotnet run --launch-profile http    # 5191
cd src-frontend && npm install && npm run dev                            # 5174
```
Never put `pkill -f "<pattern>"` in the same shell command as the pattern text itself (it killed the session shell twice);
use small helper scripts instead.

Accounts (Development): admin `ADMIN_EMAIL` / `Aadi123!` (also username `admin`), member `demo` / `Aadi123!`.

Production (2026-09-07): AWS Lightsail `aadi-prod-1` (Frankfurt, 16 GB/4 vCPU/320 GB, static IP `18.195.74.135`, IPv6
`2a05:d014:8bf:7600:99f0:159f:37b0:471`), Ubuntu 24.04, firewall 22/80/443. `deploy/server-setup.sh` prepares the host,
`deploy/deploy.sh` rsyncs and runs `docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build`;
Caddy (`deploy/Caddyfile`) terminates TLS for `aialreadydidit.com`, `www.`, `files.` (MinIO), `mcp.` and redirects
`aialreadymadeit.com`. Secrets live in `.env.production` (gitignored, generated 2026-09-07). **Deployed 2026-09-07 03:05 UTC**:
all six certificates issued, migrations applied, health 8/8. SSH key: `~/.ssh/LightsailDefaultKey-eu-central-1.pem`
(never inside the project — `*.pem` is gitignored and rsync-excluded). Production seeds reference data
and the admin only; `Seed:PublishSampleApps` (`SEED_PUBLISH_SAMPLE_APPS=true` in `.env.production`) published CPU-Z and
HWMonitor from `seed-projects/` on 2026-09-07 (admin as uploader, current dates, no demo member/ratings; idempotent on the
two slugs). `seed-projects/cpu_z` and `hw_monitor` are copies of the owner's projects without `.venv/.idea/.git`.

Migrations: `cd src-backend/AiAlreadyDidIt.Api && dotnet ef migrations add <Name> -o Data/Migrations` (the DB must be
reachable per `appsettings.json`). Rebuild before `dotnet run --no-build` — the first compose-less run failed because
the migration was regenerated after the build.

## 7. What was verified

- Backend builds; MCP builds; frontend `type-check`, `lint` and `build-only` are clean.
- `npm run test` (2026-09-06): 96 Vitest tests green — format/tools/validation helpers, BaseService envelope + URL building,
  LocalService, axios interceptors (bearer, single refresh on 401 + retry, sign-out on failed refresh, error normalisation),
  user/site/category/common/notification stores, route metas + navigation guard, `useAppBrowser`, ScoreBadge/StatusTag/
  PagePagination/MarkdownView, and EN/TR i18n parity (same keys, same `{placeholders}`, no shadowing between domain files).
- `dotnet test` (2026-09-06): 110 unit tests green — TextUtil, PBKDF2 hasher, LicenseDetector, SourceAnalyzer (zip / tar.gz /
  GitHub root-folder tarballs), RepositoryUrl, SavingsCoefficients, AadiJson, JwtTokenService, API-key extraction, embedding text.
- With curl against the API: home, hybrid/keyword/Turkish search, facets, agent verdicts (download / fork / build),
  download redirect + presigned URL + JSON variant, screenshot streaming, sign-in + cookie refresh, admin dashboard,
  health (8 checks green), full wizard flow as `demo` (draft → archive upload analysed: 2070 lines, MIT detected →
  installer upload with extension validation → screenshot → readiness → submit → ClamAV clean → pending review → LLM
  category suggestion applied), admin approve with category, lineage (fork of HWMonitor), notifications, ratings gate
  (own app blocked, downloaders allowed), API key creation and use, wanted request via API key, search analytics.
- MCP: `tools/list`, `check_before_building`, `download_app`, `prompts/list` over Streamable HTTP.
- UI: headless Chrome screenshots (the Claude Chrome extension was not connected) of home, search, category, app page,
  wanted, agents, about, login, dashboard, my apps, wizard, API keys and admin pages — all render without errors
  (Vite "Outdated Optimize Dep" 504s on the very first visit are a dev-server artefact).
- docker-compose stack end to end: nginx proxy (HTTP/1.1 fix for bodiless POSTs), crawler-rendered app page, images,
  presigned MinIO download from the host, semantic search after `reembed_all`, MCP container → API container.

## 8. Problems hit and fixes

| Problem | Fix |
|---|---|
| `Clock.Now` clashed with `AuthenticationHandler.Clock` | fully qualified `Infrastructure.Clock` in the API-key handler |
| S3 `DisablePayloadSigning` requires HTTPS | removed the flag (MinIO over http) |
| Job payloads are camelCase but handlers read PascalCase | case-insensitive payload lookup |
| `EF.Functions.WebSearchToTsQuery` computed outside the expression → client eval error | inline it in `Where`/`OrderBy` |
| `[FromQuery] bool json` rejected `json=1` | accept string `"1"`/`"true"` |
| Rating projection used a static method in `Select` → NRE | `Expression<Func<Rating,RatingDto>>` factory |
| Seed category slugs wrong (`&` → `and`) | `system-and-utilities-hardware-and-sensors-…` |
| bge-m3 similarity scale lower than assumed | duplicate threshold 0.80 → 0.72 |
| ImageSharp 4.1.1 fails Docker build (license key) | ImageSharp 3.1.12 |
| `Ai__EmbeddingProvider=""` from compose parsed as `None` | empty string inherits `Ai:Provider` |
| `/app/Files/tmp` not writable in container (volume owned by root) | Dockerfile creates/chowns `/app/Files`; existing volume chowned once |
| nginx bodiless POST → Kestrel 400 | `proxy_http_version 1.1; proxy_set_header Connection ""` |
| nginx `$1` reset by the bot `map` regex | named capture `(?<slug>…)` |
| i18n key `app_name` overridden by the wizard's label | renamed to `app_name_label` |
| Element Plus typing: `row` is `DefaultRow`, radio `null` value, cascader/tree types | casts in templates, string tri-state, `CascaderOption`, tree `Node` type |
| Npgsql warning `libgssapi_krb5.so.2` in the image | `libgssapi-krb5-2` installed in the runtime stage |
| `Slugify("İzleyici")` dropped the İ (invariant lower-casing leaves U+0130, regex then removed it) | map İ/I/ı, Ğ/ğ, Ş/ş before `ToLowerInvariant` |
| `RepositoryUrl.Normalize("ftp://…")` produced `https://ftp//…` | any scheme other than http/https → null |
| LicenseDetector 0BSD rule unreachable (ISC listed first, no `None` clause) | 0BSD before ISC, same as MIT-0 before MIT |
| MSB3277 EF Core 10.0.4 vs 10.0.11 in the test project (Design is PrivateAssets) | explicit `Microsoft.EntityFrameworkCore.Relational` 10.0.11 in the API |
| Vitest: `Unknown file extension ".scss"` when mounting components (ElementPlusResolver injects theme-chalk SCSS imports, Node loads them raw) | `vite.config.ts` uses `importStyle: false` when `mode === 'test'` |
| Vitest: `vi.mock` factory referencing a top-level component → "Cannot access before initialization" | wrap it in `vi.hoisted` |
| `vue-tsc --build` rejected `import './vite.config.ts'` in vitest.config.ts | `allowImportingTsExtensions` in tsconfig.node.json |
| Home category bar clipped "Internet & Networking" and everything after it (16 roots in an `overflow-x:auto` row with the scrollbar hidden) | `CategoryNav.vue` measures the roots and collapses the overflow into a "More" mega panel (ResizeObserver + locale watch); hidden items stay in the DOM for measuring |
| Template `ref` on `RouterLink` returns the component instance → `offsetWidth` undefined → NaN budget → nothing collapsed | resolve `$el` (`toEl()` helper) |
| Browser kept a stale `index.html` after `docker compose up --build web` (nginx sent no `Cache-Control`, Chrome cached heuristically) | `add_header Cache-Control "no-cache"` on the SPA shell locations; hashed assets stay immutable |
| QA 2026-09-06 (see `QA-REPORT.md`): keyword search unstemmed, nonsense queries matched, drafts piled up, README relative images broken, no security headers, anonymous rate limit 60/min, chunk-load failure after deploys, several UI nits | fixed in backend commit "QA fixes" + frontend commit "QA fixes"; tsvector migration `SearchVectorEnglishStemming`; `search.min_similarity` 0.45 |
| `aialreadydidit-clamav-1` shown **unhealthy** although clamd works: the image's `clamdcheck.sh` pings `localhost:3310`, Alpine resolves `localhost` to `::1` first, and our `clamd.conf` bound `TCPAddr 0.0.0.0` only | added `TCPAddr ::` (clamd accepts several `TCPAddr` lines) |
| Admin › Apps grid actions (unlist / restore / remove / feature / unfeature) → 404 "Request failed with status code 404" (owner, 2026-09-08): the route was `[HttpPost("{id:int}/{action}")]` and `action` is MVC's reserved route token, so the segment only matched the literal method name | parameter renamed to `{verb}`; `RouteTemplateTests` scans every controller for `{action}`/`{controller}`; "feature" now requires Published (422 otherwise) and assigns `FeaturedOrder` (also when approving with the feature checkbox) |
| Headless Chrome (`--headless=new --enable-logging=stderr --v=0`) prints page console messages, so CSP violations ("Refused to …") can be checked from the shell without the extension | used for the CSP verification crawl |
| Claude Chrome extension screenshots time out when the tab is in the background (`document.visibilityState === 'hidden'`) | verify via the JavaScript tool / DOM, or headless `google-chrome --screenshot` from the shell |

## 9. Seed data facts

- `/mnt/sdy1/stuff/LLM generated projects/cpu_z` — "CPU-Z for Linux" (GTK 3, Python, `.deb`, 10 screenshots in `docs/`);
  an MIT LICENSE file was added there on 2026-09-05 (owner approved).
- `/mnt/sdy1/stuff/LLM generated projects/hw_monitor` — "HWMonitor for Linux" (MIT, `.deb`, 3 screenshots).
- Neither project has a git remote, so the seed uses the archive path: a tar.gz snapshot of the working tree (without
  `.venv/.idea/.git/dist/__pycache__`) is analysed, stored in MinIO, scanned and embedded.
- Both are recorded as generated by **Anthropic Claude Fable 5.1** with the note "Claude Code session; prompts below were
  reconstructed from the README". The prompts are reconstructions, not the originals — edit them in the admin panel /
  editor if the real ones are available.
- Categories: System & Utilities › Hardware & Sensors › System Information / Hardware Monitoring. Both are featured.
- A third app "Sensor Watch" (fork of HWMonitor, uploaded by `demo`) exists only in the compose database from testing.

## 10. Open items / owner decisions

- Git: since 2026-09-07 the project root is one repository (`main`) published at https://github.com/mehmetgoren/aialreadydidit;
  the former `src-backend` and `src-frontend` repositories were merged in with their full histories (subtree merge). Never
  commit `.env`, `.env.production`, `*.pem`, `seed-projects/*` or the owner's notes `aialreadydidit.md` / `prompts.txt` — all
  gitignored. GitHub push protection blocked a `git add -A` commit on 2026-09-08 because the notes hold the Google OAuth
  client id + secret; it was amended before it ever reached GitHub. Stage files explicitly instead of `git add -A`.
- Domain: `aialreadydidit.com` registered 2026-09-07 (plus `aialreadymadeit.com` as a redirect). Production URLs should be
  `https://aialreadydidit.com`, `https://api.aialreadydidit.com` (or `/api` behind the same host) and `https://mcp.aialreadydidit.com/mcp`.
- Set a real `JWT_KEY`, passwords, public URLs, `GOOGLE_CLIENT_ID`, SMTP (`Email__Provider=Smtp`) before going public;
  `ASPNETCORE_ENVIRONMENT=Production` disables the demo seed.
- `.env` on this machine: `OLLAMA_BASE_URL=http://host.docker.internal:11434`, `OLLAMA_PULL_MODELS=bge-m3`.
  `.env.example` keeps the container defaults (`ollama:11434`, pulls bge-m3 + qwen3.8 ≈ 18 GB).
- Host Ollama on this machine has **no systemd unit** (the owner removed it) and its models live in
  `/mnt/sdx1/local_llms/ollama/.ollama/models` (76 GB: bge-m3, qwen3.8, qwen3.8:27b, gemma4:26b, gpt-oss:20b, …), not in
  `~/.ollama`. A plain `ollama serve` binds 127.0.0.1 with an empty store and the containers cannot reach it. Start it with
  `nohup docker/ollama/host-ollama.sh &` (sets `OLLAMA_MODELS`, `OLLAMA_HOST=0.0.0.0:11434`, keep-alive 30 m) before
  `docker compose up`. GPUs: RTX 4090 24 GB + RTX 2060 SUPER 8 GB.
- `Seed:LlmProjectsPath` is mounted read-only at `/seed` in compose (`LLM_PROJECTS_PATH`).
- Category suggestion latency with qwen3.8 on first call was ~40 s (model load); the frontend uses no timeout for it.
- Possible follow-ups: e-mail templates, per-user rate tiers UI polish, more SPDX licenses, i18n review of Turkish copy,
  Playwright e2e, component tests for the wizard steps and admin tables (the Vitest harness is in place).
- Frontend unit tests exist since 2026-09-06 (Vitest 5 + @vue/test-utils + jsdom, `tsconfig.vitest.json` referenced from
  `tsconfig.json` so `type-check` covers the specs). `src/i18n/{en-US,tr-TR}/admin/` are empty leftover folders.
- Backend unit tests exist since 2026-09-06 (commit 4fea325). Service-level tests against EF Core would need a PostgreSQL
  test container (pgvector/tsvector are not supported by the in-memory or SQLite providers).

## 11. Session log (chronological)

1. Explored Gemecik backend/frontend templates, the two LLM projects, CL-AI's provider pattern; wrote `ARCHITECTURE.md`;
   asked the six blocking questions; got the answers above.
2. Started dev containers (pgvector pg18, MinIO, ClamAV), pulled bge-m3, scaffolded the .NET solution, wrote entities,
   EF configurations (tsvector generated column, HNSW index), migration, reference data + seeder.
3. Wrote infrastructure (AI providers, S3 storage, ClamAV, e-mail, image processing, GitHub/GitLab importers, license
   detector, source analyzer, durable job queue + runner, JWT + API-key auth, role-action filter, settings cache, audit).
4. Wrote domain services and controllers (account, site, catalog, search, apps/editor, downloads, ratings, reports,
   dashboard, requests, agent, SEO, jobs, all admin areas), `Program.cs`, the demo `DbSeeder`; fixed the issues in §8;
   smoke-tested everything with curl.
5. Wrote the MCP server, Dockerfiles, `docker-compose.yml`, ClamAV/Ollama configs, `.env.example`, backend README.
6. Scaffolded the frontend from the Gemecik template, wrote models, services, stores, router, styles, layouts,
   components, all pages (catalog, account, dashboard, wizard, admin) and EN/TR i18n (~715 keys each); type-check, lint,
   build green; headless-Chrome screenshots.
7. Ran the compose stack end to end, fixed the nginx/volume/provider issues, wrote frontend + root READMEs and this file.
8. Owner asked for the UI port (9002 via docker-compose, 5174 with `npm run dev`), then for `git init` + commit of
   `src-backend` and `src-frontend`: done as two separate repositories (133 and 184 files), then this file was updated.
9. "Let's continue": wrote `AiAlreadyDidIt.Tests` (xunit, 110 tests over the pure logic), which exposed and fixed the three
   bugs listed in §8 (Turkish İ in slugs, ftp:// repo URLs, unreachable 0BSD rule); solution builds with 0 warnings;
   backend committed as 4fea325. Frontend untouched.
10. "Let's continue" (again): added Vitest to the frontend — 19 spec files / 96 tests over utils, services, axios boot,
    stores, router, `useAppBrowser`, four common components and EN/TR i18n parity; fixed the three harness issues in §8;
    type-check, lint, build and tests green; frontend README documents the suite. Committed in `src-frontend` (c73e2cf).
11. "Re-init and up the docker compose": `docker compose down`, removed the `aadi-db`, `aadi-minio`, `aadi-files` volumes
    (kept the ClamAV signature and container-Ollama caches), found host Ollama stopped with its models on the sdx1 store,
    wrote `docker/ollama/host-ollama.sh` and started it, `docker compose up --build -d`. Verified: fresh migration + seed
    (2 apps, admin + demo), web 9002, hybrid/semantic search, agent verdict (fork, 0.71), presigned download redirect,
    crawler-rendered app page, 7 MCP tools, admin health 8/8 green (pgvector 0.8.6, ClamAV 1.5.4, bge-m3, qwen3.8).
12. Owner reported the home category bar cutting off "Internet & Networking" (checked with the Claude Chrome extension,
    now connected): rewrote `CategoryNav.vue` with a "More" overflow panel, fixed the nginx SPA-shell caching, verified at
    390 / 1024 / 1568 px (headless Chrome) and in the live tab (1f4ffbb). Owner then noticed a gap before "More": the
    reserve was a guessed 84 px while the button is 65 px and the row was 9 px short of fitting the 8th item — now the
    button is always rendered (hidden) and measured, item padding is 8 px, and `justify-content: space-between` spreads
    any slack (second frontend commit). Note: extension screenshots taken within ~1 s of opening the mega panel show it
    see-through (frame captured before paint); a second capture is correct.
13. Owner: LLM auto-categorisation must be optional (GPU cost), default off, manual category + sub-category otherwise.
    Done: `Ai:EnableCategorySuggestions=false` default + compose/env wiring, `AppEditorService` guards (job enqueue,
    stricter readiness), `CategoryPicker.vue` (cascading selects, 5 Vitest tests → 101), i18n keys, backend README row.
14. ClamAV container "unhealthy" (IPv6 localhost) fixed. Then a full QA pass in Chrome (owner's admin session shared the
    profile; extension screenshots need the tab in the foreground): storefront, member dashboard, wizard end to end
    (archive → install file → screenshot → submit → scan → approve → published → searchable), admin pages, HTTP/SEO
    checks. 18 findings fixed (rate limit, security headers, vite preload reload, english+simple tsvector + migration,
    semantic floor 0.45, draft reuse, README image rewriting, plural/labels/toasts), 6 left as decisions — all in
    `QA-REPORT.md`. Tests: backend 110, frontend 103.
15. Owner: add es, pt, de, fr, ar, ru, ja, ko, zh. Built the locale registry (switcher badge instead of flag SVGs, browser
    detection, dayjs/Intl/Element Plus locales, RTL `dir`), backend locale normalisation (+18 tests → 128), parity tests
    over all locales; nine parallel translation agents (sonnet) wrote the `src/i18n/<code>/` folders. Non-English
    languages load lazily (main bundle 149 kB, ~35 kB per language chunk). Tests: backend 128, frontend 108.
16. Hosting: compared AWS/Azure/Turkish VDS/Hetzner; owner chose AWS Lightsail 16 GB Frankfurt, bought aialreadydidit.com
    (+ aialreadymadeit.com redirect), created `aadi-prod-1` with static IP 18.195.74.135. Wrote the production overlay
    (Caddy TLS, no published internal ports, restart + log limits), `.env.production` with generated secrets,
    `deploy/server-setup.sh`, `deploy/deploy.sh`; nginx now forwards the original scheme from the TLS proxy.
17. Published the two sample apps on production via `Seed:PublishSampleApps` (DbSeeder `sampleAppsOnly`), staged copies
    in `seed-projects/`, fixed the seeder's idempotence (an admin draft had blocked it), health check made
    capability-aware, HSTS added in Caddy. Live: 2 featured apps, scans clean, embeddings done, downloads via
    files.aialreadydidit.com. The owner's stray production draft `cpuz-linux-1-0-0-source` (id 1) was left in place.
18. Published to GitHub (https://github.com/mehmetgoren/aialreadydidit, `main`, 21 commits): root monorepo with both
    histories subtree-merged, security sweep (no secrets/notes/personal e-mail tracked; production guard against placeholder
    secrets), MIT LICENSE, GitHub-style README with production screenshots in `docs/screenshots/`.
19. Market check (2026-09-07): no exact match for the name or the concept. Closest neighbour is onesvibe.app (~10k live
    vibe-coded *web* apps, semantic search JSON API + MCP tool `search_similar_projects`, CC0 daily JSON export) — links to
    hosted demos only, no downloads/source/prompts/license checks/lineage. Others are plain galleries (youraiproject.com,
    vibcod.dev, builtwithvibecode.com, vibecodingshowcase.com, awesome lists) or agent-tool registries (skills.sh, mcp.so).
    Possible follow-ups: import onesvibe's open-source-flagged entries as "wanted"/"also exists" hints; lead marketing with
    "download or fork it, with license + prompt + scanned installer".
20. "Let's continue" (2026-09-08): closed two open items without owner input — installer magic-byte checks
    (`InstallerSignature` + 51 xunit tests → 179, wired into `StoreInstallerFromTempAsync` so uploads and release-asset imports
    are both covered; verified via the API: text-as-.deb and HTML-as-.exe → 422 with a specific message, the real seed .deb →
    200) and the nginx Content-Security-Policy (snippet file, per-location include, Scalar excluded). Verified on the local
    compose stack: headers present on SPA/app/asset/API routes, absent on `/scalar`; headless-Chrome crawl of home, search,
    app, category, login, wanted, agents, about and the bot-rendered app page → zero CSP violations, pages render fully.
    Deployed to production the same day (owner asked); verified live: CSP on SPA/app/API routes, not on /scalar, crawl clean.
21. Owner: publish `/mnt/sdy1/stuff/LLM generated projects/audio_format_selector` to production. Added an MIT `LICENSE` to that
    project (its `debian-copyright` already declared MIT), captured the GTK window on the owner's desktop (`xwininfo` + Gdk
    pixbuf, no xdotool on this machine), and drove the wizard in the owner's signed-in Chrome tab via the extension (reading
    the session token from localStorage is blocked by the auto-mode classifier; `file_upload` accepts scratchpad paths).
    Published as https://aialreadydidit.com/app/audio-format-selector (Media › Audio & Music, MIT, Linux .deb, 1 screenshot,
    prompt reconstructed from the README, model recorded as Claude Fable 5.1 — owner to correct if different). Observed:
    the admin review page takes ~17 s on production because `AdminModerationService.DetailAsync` embeds the app text via the
    CPU-only Ollama container for the "similar apps" panel (and again after approve) — reuse the stored embedding instead.
22. Owner (2026-09-08): admin apps grid actions 404 → `{action}` route-token fix (c6783f6, deployed); "Long Description should
    be Markdown editor" → `components/common/MarkdownEditor.vue` (toolbar + Write/Preview via MarkdownView, Ctrl/⌘+B/I,
    char count, no new dependency) replaces the textarea in the wizard Details step and the admin app editor; keys
    `md_*` in `common.ts` of all 11 locales; 8 Vitest cases (frontend 116). Deployed (e33a7e0).
23. Google sign-in + SMTP (2026-09-08): compose now passes `Email__*` from `EMAIL_*` (documented in `.env.example` and the
    README "Configuration" section, which has step-by-step guides for both); admin System health has an "E-mail" check (red
    in Production while `Console`) and a **Send test e-mail** button (`POST admin/health/test-email`, sends to the signed-in
    admin, SMTP error shown verbatim). Owner created an OAuth *Web* client (origin https://aialreadydidit.com, no redirect
    URIs); only the client id was taken from the downloaded JSON (secret unused) → `GOOGLE_CLIENT_ID` in `.env.production`,
    deployed, "Continue with Google" renders on /login with zero CSP violations. Owner still needs to test a real Google
    sign-in (if "access blocked": publish the OAuth consent screen) and fill the `EMAIL_*` values (currently Console).
24. Launch research (2026-09-08, owner asked whether/where to announce on Reddit): rules read via the Reddit JSON endpoints in
    the owner's browser. Recommendation: **Show HN first**, Reddit second; before either, grow the catalogue from 5 to
    20–30 apps (GitHub import + owner's projects) and post from an account with history. Ranked: r/ClaudeAI (explicitly
    encourages Claude-built showcases, flair), r/opensource (LICENSE required, promote "to a degree"), r/SideProject,
    r/vibecoding (must explain how it was built), r/coolgithubprojects; r/selfhosted only via the New Project Megathread;
    r/LocalLLaMA and r/mcp later. Avoid r/ChatGPTCoding (weekly promo thread only, FOSS included), r/programming (no
    "I made this"), r/webdev (Showoff Saturday only), r/InternetIsBeautiful (no aggregators/stores), r/artificial.
    Also: Product Hunt, dev.to/Hashnode article, fosstodon, Anthropic Discord showcase, awesome-mcp-servers. Next step
    offered: draft the Show HN + r/ClaudeAI posts and import candidate apps.
25. "Let's continue" (2026-09-11): (a) review-page latency fixed — `AdminModerationService.DetailAsync` now queries pgvector
    with the app's stored vector (`SearchService.NearestToVectorAsync`) instead of embedding the text on every open; a missing
    vector queues `embed_app` once (`EnqueueOnceAsync`) and the panel stays empty until the job ran; `SubmitAsync` queues
    `embed_app` so the vector exists before a moderator opens the submission (previously only `PublishAsync` did). Verified on
    the local compose stack: detail for CPU-Z 0.3 s with HWMonitor at 0.738, detail for a vector-less draft 0.03 s + job
    queued and done. Backend 180 tests green. Commit 84cff25, **not yet deployed**. (b) Launch post drafts written to
    `docs/launch/posts.md` (Show HN title + maker comment + prepared answers, r/ClaudeAI, r/opensource, r/SideProject,
    r/vibecoding, r/coolgithubprojects, Product Hunt tagline, article outline, posting order and pre-flight checklist).
    (c) Deployed 84cff25 (first `deploy.sh` run hung 12 min in the API image's `apt-get update` on a dead mirror connection —
    killed the remote `docker compose`/`buildx` processes and reran; `tail` on the script hides output until exit, log to a
    file instead). (d) **SMTP live via Brevo**: owner created the account; `.env.production` now has `EMAIL_PROVIDER=Smtp`,
    `smtp-relay.brevo.com:587` STARTTLS, login `b8eef6001@smtp-brevo.com`, sender `no-reply@aialreadydidit.com`; deployed;
    STARTTLS + AUTH verified from the host with openssl; admin health "E-mail" check green; "Send test e-mail" → 200 (nginx log
    15:30 UTC) but Brevo rejected it ("sender … is not valid") because the domain was not authenticated. Fixed the same day via
    the Chrome extension in the owner's signed-in Brevo + Cloudflare tabs: Brevo Domains → add `aialreadydidit.com` → Manual
    setup; four records added at Cloudflare (DNS at Cloudflare, account of the admin e-mail): TXT `@` `brevo-code:be190e12…`,
    CNAME `brevo1._domainkey` → `b1.aialreadydidit-com.dkim.brevo.com`, CNAME `brevo2._domainkey` → `b2.…` (both **DNS only**),
    TXT `_dmarc` `v=DMARC1; p=none; rua=mailto:rua@dmarc.brevo.com`; Verify records → all matched → Authenticate domain →
    "Your domain has been authenticated"; sender "AI Already Did It <no-reply@aialreadydidit.com>" added. Resent the test
    e-mail: Brevo log 18:51 local shows Sent → Delivered → opened. No MX/SPF on the domain (Brevo does not need SPF for the
    From domain; inbound mail to @aialreadydidit.com does not exist). Brevo's "Block unauthorized IP addresses" dialog is the
    optional IP allowlist, not key activation. Extension quirks: `navigate` to a domain the owner did not grant for that tab
    fails with "Navigation to this domain is not allowed" — create a fresh tab instead; Cloudflare's Add-record dialog shifts
    down when the preview sentence wraps, so re-screenshot before clicking the proxy toggle.
26. Owner (2026-09-11): (1) admin left panel had no scrollbar — `.admin__aside` was `overflow: hidden` with the menu taller than
    the viewport; the aside is now a flex column with a scrollable `.admin__menu` wrapper (brand row fixed), `LeftMenu`
    `min-height: 100%`. (2) Savings counter "too optimistic": the 728 M tokens on production came almost entirely from
    uploader-typed totals (`est_is_override`) — Mint Paint alone claimed 151 531 580 tokens (a Claude Code session total incl.
    cache reads) × 4 downloads ≈ 606 M; the size heuristic (36 tokens/line) was already modest. Changes ("savings realism"):
    new `apps.est_claimed_tokens` keeps the claim, `est_generation_tokens` holds the value the counter uses =
    `min(claim, heuristic(max(lines,200)) × savings.override_cap_factor)` (default 5); cost is always derived (wizard USD input
    removed); the counter multiplies **distinct (user, IP) downloaders** per app (not raw downloads) by `savings.reuse_share`
    (default 0.5 — half of the downloads assumed to replace a generation); defaults `price_per_million_tokens` 15 → 6 and
    `kwh_per_million_tokens` 0.4 → 0.3 (migration `SavingsRealism` moves them only if still at the old default, caps existing
    overrides and recomputes every cost); per-app "saved so far", dashboard and admin stats use the same `Saved()`; new
    maintenance task `recompute_estimates` (Admin › Jobs) re-applies the coefficients to all apps. `SavingsDto` gained
    `uniqueDownloads` + `reuseShare`; copy updated in 11 locales (`savings_hint`, `about_how_counter_text`, `adm_formula`,
    `cost_hint`, new `adm_recompute_estimates`). Verified locally: migration applied, claim 151 531 580 on a 4 147-line app →
    746 460 stored, counter 301 698 tokens from 5 unique of 8 downloads. Tests: backend 183, frontend 116. Headless
    verification trick: `scratchpad/cdp-shot.mjs` (Node 24 built-in WebSocket + CDP) signs in through the API and writes
    `localStorage.aadi_user` the way the SPA does, then screenshots any admin page — no Playwright needed.
27. Owner (2026-09-11): several install files per platform (e.g. `.deb` + `.AppImage`). Before, `StoreInstallerFromTempAsync` /
    `AddExternalFileAsync` deleted every file of that platform on upload and `UpdateFileAsync` refused a platform that already
    had one. Now only a file with the **same name** (case-insensitive) on the same platform is replaced (`ReplaceSameNameAsync`);
    readiness still needs ≥ 1 per declared platform; `DownloadButtons.vue` appends the extension to the label when a platform
    has more than one file; MCP `download_app` with a platform picks the first file and lists the rest under `alternatives`;
    `files_rule_hint` rewritten in 11 locales. Verified via the API as `demo`: two Linux .deb files coexist, re-upload of the
    same name replaced it. Rule text in the brief ("at least one file per declared platform") is unchanged.
    Chrome extension note: after a Chrome restart two extension instances were "connected"; the stale one kept the old tab
    ids and a signed-out profile — use `list_connected_browsers` + `select_browser` (ask the owner which) before assuming
    the cookie is missing. Observed: "Continue with Google" rendered twice on the login page after a second SPA navigation.

## 12. Where things stand (2026-09-08)

- Production live at https://aialreadydidit.com (Lightsail Frankfurt, 18.195.74.135): 2 featured apps, health 8/8,
  admin password was changed by the owner after first sign-in (initial one is burned — it was shown in chat).
- Code on GitHub: https://github.com/mehmetgoren/aialreadydidit (`main`), everything committed and pushed. Deployed 2026-09-08
  (CSP + installer sniffing): headers live, health 200, headless-Chrome crawl of 7 production pages → zero CSP violations.
- Redeploy: `SSH_KEY=~/.ssh/LightsailDefaultKey-eu-central-1.pem deploy/deploy.sh` from the project root.
- Production content (2026-09-08): 5 published apps — CPU-Z, HWMonitor, Audio Format Selector (all admin), Mint Paint
  (admin), AdBlock (member `hakanss`); drafts `paint` (5) and `cpuz-linux-1-0-0-source` (1). Google sign-in live.
- Next session candidates: (1) ~~SMTP~~ done 2026-09-11; (2) grow the catalogue to 20–30
  apps before announcing; (3) owner reviews `docs/launch/posts.md`, then post (Show HN first); (4) owner confirms the test mail
  is in the Gmail inbox (not spam) and shows "signed by aialreadydidit.com"; (5) duplicated Google button on /login after SPA re-navigation; (6) Markdown
  editor for changelog fields if wanted; (7) tell uploaders in the wizard the capped value that will be used.
- Open: native-speaker review of the 9 LLM translations, per-language category names, GitHub repo topics/homepage/
  secret scanning (owner to click), the owner's stray production draft `cpuz-linux-1-0-0-source` (id 1).
