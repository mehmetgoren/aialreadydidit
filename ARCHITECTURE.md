# AI Already Did It — architecture & data-model plan (v0, pre-code)

Status: draft for review. Code starts after the open questions at the bottom are answered.

## 1. Repository layout (mirrors Gemecik exactly)

```
AiAlreadyDidIt/
  docker-compose.yml                  db + minio + clamav + embeddings + api + mcp + web   (one command)
  src-backend/                        own git repo, like Gemecik/src-backend
    AiAlreadyDidIt.sln
    AiAlreadyDidIt.Api/               ASP.NET Core 10 Web API, classic controllers, EF Core 10 + Npgsql
      Contracts/<Domain>/             request / DTO records            (Common: ApiResponse envelope, PagedResult, ApiException, ErrorCatalog)
      Controllers/V1/<Domain>/        thin controllers → services
      Data/{Configurations,Migrations,Seed}  DbContext, IEntityTypeConfiguration per entity, migrations (reference data inserted in migration), DbSeeder (dev demo)
      Entities/                       POCOs
      Infrastructure/                 Options, CurrentUser, Middleware, Clock, Json, PasswordHasher, RateLimiting, hosted jobs
      Services/<Domain>/              one service per controller, auto-registered by "*Service" convention
      Dockerfile, appsettings*.json, Properties/launchSettings.json
    AiAlreadyDidIt.Mcp/               MCP server (ModelContextProtocol C# SDK), Streamable-HTTP + stdio; thin client of the public REST API
  src-frontend/                       own git repo, like Gemecik/src-frontend
    src/{boot,i18n/{tr-TR,en-US},layouts,components,pages,router/routes,stores,utils/{models,services,validation}}
    Dockerfile (node build → nginx), docker/nginx.conf (proxies /api, /files, bot pre-render)
```

Conventions carried over verbatim: Farmazon-style envelope `{statusCode, statusMessage, result, errors}`, `ApiException` → middleware, snake_case DB naming, `[Authorize] + [RoleActionAuthorize]` admin filter with Role/Menu/RoleMenu/RoleAction tables, `BaseService` per controller on the frontend, `LocalService`, Pinia setup stores, flat snake_case i18n keys per domain, `AdminPage`/`AdminDataTable`/`LeftMenu` components, `StorefrontLayout` / `ProfileLayout` (renamed *DashboardLayout*) / `AdminLayout`.

## 2. Services (docker-compose)

| Service | Image | Purpose |
|---|---|---|
| `db` | `pgvector/pgvector:pg17` | PostgreSQL 17 + pgvector extension (`vector`, `pg_trgm`, `unaccent`) |
| `minio` | `minio/minio` | S3-compatible object storage; buckets `screenshots` (public-read), `installers`, `sources` (private, presigned) |
| `clamav` | `clamav/clamav` | clamd on 3310; API streams files with INSTREAM (nClam) |
| `embeddings` | `ollama/ollama` (pulls `bge-m3` on first start) | text → 1024-d vector, TR + EN. Pluggable (`Embeddings:Provider = Ollama | OpenAI | Onnx | None`) |
| `api` | built from `src-backend` | REST API, background workers (scan, import, embed, notify) |
| `mcp` | built from `src-backend` (Mcp project) | MCP server on :5191 (`/mcp`), talks to `api` |
| `web` | built from `src-frontend` | nginx: SPA + reverse proxy `/api`, `/files`; crawler UAs on `/app/*` → `api:/seo/app/*` |

Ports (host): web 9002, api 5190, mcp 5191, minio 9000/9090, db 5433 (avoids colliding with Gemecik on 5182/9001).

## 3. Search: PostgreSQL + pgvector + tsvector (decision)

OpenSearch is **not** justified here: the corpus is apps (thousands → maybe a few hundred thousand), documents are short (name + descriptions + tags + README excerpt), and the decisive filters (category subtree, platform, license, model, rating, status=Published) are relational. pgvector HNSW with cosine distance answers k-NN in milliseconds at that scale, `tsvector` + `pg_trgm` cover keyword/typo search, and everything stays transactional with moderation status (no dual-write, no re-index lag, no 2 GB JVM). Migration path if it ever grows: the `ISearchIndex` abstraction has one implementation; OpenSearch would be a second.

Hybrid ranking: `score = RRF(keyword_rank, vector_rank)` with boosts for exact name match, rating and download count; both lists are pre-filtered by the same category/platform/license/model predicates. Language: one `tsvector` with `simple` + `unaccent` config (works for TR and EN without stemming mistakes), `bge-m3` embeddings are multilingual.

Embedded text per app = `name | short_description | long_description | tags | category path | README (first 4 KB)`; re-embedded when any of those change (background job, `embedding_stale` flag).

Duplicate detection at upload = same vector search on the draft text (debounced 600 ms from the form). Threshold (default 0.80 cosine similarity, admin setting) → banner: "This looks ~85% similar to X — contribute to it or declare yours as a fork?" → buttons fill `derived_from_app_id`.

## 4. Data model (PostgreSQL, snake_case)

Identity
- `users` (id, email UNIQUE, email_verified_at, username UNIQUE, display_name, password_hash NULL for OAuth-only, avatar_url, bio, website, role_id, is_active, is_banned, ban_reason, locale, last_login_at, created_at)
- `roles`, `menus`, `role_menus`, `role_actions` (Gemecik admin RBAC, unchanged)
- `external_logins` (user_id, provider = google, provider_user_id, email)
- `refresh_tokens` (user_id, token_hash, expires_at, created_at, created_ip, user_agent, revoked_at, replaced_by) — rotating; doubles as "sessions"
- `email_tokens` (user_id, kind = Verify|Reset, token_hash, expires_at, used_at)
- `api_keys` (user_id, name, prefix `aad_…8`, key_hash, scopes[], rate_tier, last_used_at, expires_at, revoked_at)
- `api_key_usage_daily` (api_key_id, date, request_count, download_count)

Reference lists (admin-managed, seeded in migration)
- `categories` (id, parent_id, level 1..3, slug, name_tr, name_en, description, icon, sort_order, is_active, app_count cached)
- `platforms` (code = windows|linux|macos|web|android|ios|docker|cli, name, icon, allowed_extensions[], sort_order, is_active)
- `licenses` (spdx_id, name, url, is_osi_approved, is_fsf_libre, is_allowed, detection_fingerprint) — ~35 common SPDX ids seeded
- `llm_models` (vendor, name, version, slug, released_on, is_active) — Claude Opus 4.1 / Sonnet 4.5 / Fable 5.1, GPT-5, Gemini 2.5 Pro, … editable
- `tags` (name, slug, usage_count, is_blocked)

Catalogue
- `apps` (id, slug UNIQUE, name, short_description ≤160, long_description md, category_id, license_spdx_id, uploader_user_id, llm_model_id, derived_from_app_id NULL, derivation_kind = Fork|Inspired|Port, repo_url, repo_provider = GitHub|GitLab|Archive, repo_default_branch, repo_stars, repo_primary_language, repo_synced_at, icon_url, homepage_url, status = Draft|PendingScan|PendingReview|Published|Rejected|Unlisted|Removed, rejection_reason, published_at, latest_version_id, search_vector tsvector, embedding vector(1024), embedding_stale, est_generation_tokens, est_generation_cost_usd, est_source_override bool, rating_avg, rating_count, download_count, view_count, is_featured, featured_order, created_at, updated_at)
- `app_versions` (app_id, version string, changelog md, released_at, source_kind = RepoTag|RepoCommit|Archive, source_ref, source_file_id (snapshot zip in MinIO), status, created_by, created_at)
- `app_files` (version_id, platform_id, kind = Installer|Source|DockerImage|WebBundle, file_name, storage_key, size_bytes, sha256, content_type, external_url NULL (docker image ref / release asset), scan_status = Pending|Clean|Infected|Error, scan_signature, scanned_at, download_count)
- `app_screenshots` (app_id, storage_key, width, height, caption, sort_order) — ≥ 1 required, 3 recommended, max 10, image types only, re-encoded server-side
- `app_prompts` (app_id, version_id NULL, title, prompt_text, sort_order) — the original prompt(s)
- `app_tags` (app_id, tag_id)

Engagement
- `downloads` (id, app_id, version_id, file_id, user_id NULL, api_key_id NULL, source = Web|Api|Mcp|Seo, ip_hash, user_agent, country NULL, created_at) — gate for rating; feeds counters and savings
- `ratings` (app_id, user_id UNIQUE pair, score 0..100, review ≤2000, version_id, worked bool, helpful_count, status = Visible|Hidden, created_at, updated_at)
- `rating_replies` (rating_id, user_id (uploader), body) — Play Store "developer reply"
- `rating_votes` (rating_id, user_id, is_helpful)
- `favorites` (user_id, app_id)
- `collections` (user_id, name, slug, description, is_public) + `collection_items` (collection_id, app_id, note, sort_order)
- `app_watches` (user_id, app_id, notify_new_version, notify_replies)
- `notifications` (user_id, type, title, body, link, read_at, created_at) — new version, moderation result, report outcome, reply to my review
- `reports` (app_id, rating_id NULL, reporter_user_id NULL, reason = Malware|NotOpenSource|NotFree|Copyright|Broken|Spam|Other, details, status = Open|Reviewing|Resolved|Dismissed, handled_by, resolution, created_at)
- `search_logs` (query, mode = Keyword|Semantic|Hybrid, filters json, result_count, top_similarity, source = Web|Api|Mcp, user_id/api_key_id NULL, took_ms, created_at) — powers "top / zero-result searches"
- `app_requests` ("wanted" board: title, description, embedding, requester_user_id, status = Open|Fulfilled|Closed, fulfilled_by_app_id, vote_count) — what agents/people looked for and did not find

Moderation & platform
- `moderation_actions` (app_id, version_id NULL, admin_user_id, action = Approve|Reject|Unlist|Restore|Rescan|OverrideLicense|Feature, note, created_at)
- `scan_results` (file_id, engine, verdict, signature, raw, scanned_at)
- `site_settings` (key/value/group/type — thresholds, limits, savings formula, announcement, SEO texts)
- `featured_apps`, `banners` (home carousel), `audit_logs`, `background_jobs` (type, payload, status, attempts, last_error, run_at) — durable queue for scan / import / embed / notify

Indexes: HNSW on `apps.embedding` (cosine), GIN on `search_vector`, GIN trigram on `name`, partial index on `status = Published`, `(category_id, status)`, `downloads(app_id, created_at)`.

## 5. Upload pipeline (strict, client + server)

1. **Draft** — form wizard (5 steps): Source → Details (name, descriptions with live duplicate check, category, tags, license, model, prompts, lineage) → Platforms & files → Screenshots → Review & submit.
2. **Source**: `POST /apps/import/inspect {repoUrl}` fetches metadata via GitHub/GitLab REST (README, LICENSE + SPDX from the license API, primary language, stars, releases with assets, tags/changelog) and pre-fills the form; a tarball of the default branch / chosen tag is snapshotted into `sources/`. Fallback: archive upload (zip/tar.gz ≤ 200 MB) — server opens it, finds `LICENSE*|COPYING*`, matches text against the fingerprint of the declared SPDX id (mismatch → 422 with both ids). Rules: license must be `is_allowed`; archive must not be empty / binary-only (source file ratio check).
3. **Files**: at least one file per declared platform; extension whitelist per platform (`.exe .msi` / `.deb .rpm .AppImage .tar.gz` / `.dmg .pkg` / `.apk` / `.ipa` / docker image ref `registry/image:tag` / web bundle zip or URL / CLI archive). Uploads go straight to MinIO through presigned PUT (multipart for large), the API records `app_files` in `Pending`.
4. **Screenshots**: ≥ 1 (site setting, default 1, UI recommends 3), PNG/JPG/WebP ≤ 5 MB, re-encoded to WebP + thumbnail.
5. **Submit** → server re-validates everything → `PendingScan` → ClamAV job scans every file (Infected → auto-reject + notify + report row) → `PendingReview` → moderation queue → `Published` (embedding job runs on publish and on edits).
6. **New version**: same wizard minus details; versions of already-published apps go through scan + (setting) review or auto-publish for trusted uploaders.

## 6. Public API & agent surface

- REST `api/v1/…` (envelope, OpenAPI at `/openapi/v1.json`, Scalar UI): `catalog/*` (categories, home, featured, trending, new), `search` (`q, mode, category, platform, license, model, minRating, tags, sort, page`), `apps/{slug}` (+ `versions`, `files`, `ratings`, `similar`, `lineage`), `apps/{slug}/download/{fileId}` (records download, 302 → presigned URL), `agent/check` (`q` → verdict `Download|Fork|Build` + best matches + similarity — the one call an agent needs), `agent/request` (log a "wanted" entry).
- Auth for agents: `X-Api-Key` (or Bearer). Rate limits (site settings): anonymous 60 req/min per IP, key tier 600 req/min, downloads 30/min; 429 with `Retry-After`.
- MCP tools: `search_apps`, `get_app`, `list_categories`, `download_app` (returns presigned URL + sha256 + install hint), `check_before_building`, `submit_app_request`. Resources: `aad://categories`, `aad://app/{slug}`. Prompt: "reuse-first".
- SEO: `GET /seo/app/{slug}` (server-rendered HTML: meta, Open Graph, JSON-LD `SoftwareApplication`, full description, screenshots, versions) served to crawler UAs by nginx; `sitemap.xml`, `robots.txt`, `llms.txt`. Humans get the SPA whose `<head>` is patched client-side too.

## 7. Frontend pages

Storefront: Home (mission hero + live savings counter, featured carousel, trending, new & updated, categories grid), Category (sahibinden-style left tree + facets: platform, license, model, rating, tags; sort), Search (hybrid, "meaning / keyword" toggle, similarity badge), App detail (gallery, install buttons per platform, README, prompts with "copy prompt" and "make my own variant", versions/changelog, ratings with worked/didn't-work bars, lineage tree, similar apps, report), Uploader profile, Collection (public), Wanted board, Login/Signup (email + Google), Verify/Reset.
Dashboard (`/dashboard`, ProfileLayout): My apps (+ new upload wizard, new version, stats per app), My reviews, Downloads, Favorites & collections, Watching, Notifications, API keys, Settings.
Admin (`/admin`): Dashboard (KPIs, charts), Moderation queue (preview, file list with scan verdicts, license check, approve/reject/request changes), Apps, Versions/files (rescan, remove), Reports, Users (roles, ban, sessions), Categories (tree, move/merge), Tags (merge/block), Licenses, Platforms, LLM models, Featured & banners, Announcements, Site settings (thresholds, limits, savings formula, rate tiers), Statistics (downloads, searches, zero-result queries, savings, top apps/uploaders), API keys (all users, usage), Audit log, Background jobs, System health (db, minio, clamav + signature age, embeddings, mcp, disk).

## 8. Savings counter

Per app: `est_generation_tokens = LOC × tokens_per_loc(12) × iteration_factor(3)` computed from the source snapshot (uploader may override with real numbers from their session; admin coefficients in site settings), `est_generation_cost_usd = tokens × blended_price_per_mtoken`. Site savings = Σ over downloads of the app estimate; also shown as kWh / CO₂ using admin-configurable constants. Cached materialised numbers refreshed every 5 min; the home page animates the counter.

## 9. Security

PBKDF2 passwords, JWT access 15 min + rotating refresh token (httpOnly cookie, 30 days), Google Sign-In via ID-token verification (`Google.Apis.Auth`), email verification, rate limiting on every public endpoint, presigned URLs (10 min), file type sniffing (magic bytes) not just extension, ClamAV before publish, size limits, CSP headers on nginx, audit log on every admin action, report button everywhere, IP hashing in download logs.

## 10. Seed data

Two apps from the owner's LLM-generated projects (`seed-projects/`): **CPU-Z for Linux** (cpuz-linux, GTK 3, `.deb`, 10 screenshots in `docs/`) and **HWMonitor for Linux** (hwmonitor-linux, MIT, `.deb`, 3 screenshots). Seed uploads them through the real pipeline (archive path, snapshot built from the working tree without `.venv/.idea/.git/dist`) so scan/license/embedding all run for real. Admin account + a demo member account (Development only).
