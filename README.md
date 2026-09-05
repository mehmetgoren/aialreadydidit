# AI Already Did It — backend

Two ASP.NET Core 10 projects (classic controllers, Entity Framework Core 10, PostgreSQL + pgvector):

| Project | Purpose |
|---|---|
| `AiAlreadyDidIt.Api` | REST API for the storefront SPA, the member dashboard, the admin panel and LLM agents. Hosts the background workers (ClamAV scan, repository import, embeddings, notifications). |
| `AiAlreadyDidIt.Mcp` | MCP server (Streamable HTTP at `/mcp`, or `--stdio`) exposing `check_before_building`, `search_apps`, `get_app`, `list_categories`, `download_app`, `submit_app_request`, `get_savings`. It is a thin client of the API. |

## Running locally

```bash
# dependencies (or use ../docker-compose.yml for everything)
docker run -d --name aadi-dev-db -e POSTGRES_PASSWORD=1234 -e POSTGRES_DB=aialreadydidit -p 5433:5432 pgvector/pgvector:pg18
docker run -d --name aadi-dev-minio -e MINIO_ROOT_USER=minioadmin -e MINIO_ROOT_PASSWORD=minioadmin -p 9000:9000 -p 9090:9090 minio/minio server /data --console-address ":9090"
docker run -d --name aadi-dev-clamav -p 3310:3310 clamav/clamav
ollama pull bge-m3          # embeddings (host Ollama on :11434); qwen3.8 for category suggestions

cd AiAlreadyDidIt.Api && dotnet run --launch-profile http     # http://localhost:5190
cd AiAlreadyDidIt.Mcp && dotnet run --launch-profile http     # http://localhost:5191/mcp
```

On startup the API applies migrations (`Database:MigrateOnStartup`), inserts the reference lists (categories, platforms, SPDX
licenses, LLM models, roles, admin menus, settings — `Data/Seed/ReferenceData.cs`, idempotent), creates the administrator
(`Admin:Email` / `Admin:InitialPassword`), the MinIO buckets, and in Development seeds the demo member plus the two demo apps from
`Seed:LlmProjectsPath` (`Data/DbSeeder.cs`).

* OpenAPI: `GET /openapi/v1.json` — interactive docs (Scalar): `GET /scalar/v1`
* Agent guide: `GET /api/v1/agent`, `GET /llms.txt`; one-call check: `GET /api/v1/agent/check?q=...`
* Images are served through `GET /files/{screenshots|icons|banners}/...`; downloads redirect to presigned MinIO URLs.
* Crawler HTML for app pages: `GET /seo/app/{slug}`, plus `/sitemap.xml`, `/robots.txt`.

### Accounts (Development seed)

| Login | Password | Role |
|---|---|---|
| `ioniangamer@gmail.com` (`admin`) | `Aadi123!` (`Admin:InitialPassword`) | Admin |
| `demo` | `Aadi123!` | Member (uploaded a fork, rated the demo apps) |

## Configuration (`appsettings.json`, overridable with `Section__Key` environment variables)

| Section | Keys |
|---|---|
| `Jwt` | `Key` (≥ 32 bytes), `AccessTokenMinutes` (15), `RefreshTokenDays` (30, httpOnly cookie) |
| `Site` | `PublicUrl`, `ApiPublicUrl`, `McpPublicUrl`, `GoogleClientId` (empty = button hidden), `RequireEmailVerification`, `DefaultLocale` |
| `Ai` | `Provider` = `Ollama` \| `OpenAi` \| `None`; optional `EmbeddingProvider` / `ChatProvider` overrides; `EmbeddingDimensions` (1024); `Ollama:*`, `OpenAi:*` |
| `Storage` | S3 endpoint (inside docker `http://minio:9000`), `PublicEndpoint` (what browsers reach), keys, size limits |
| `ClamAv` | `Host`, `Port`, `FailClosed` (keep files pending while clamd is down) |
| `RateLimiting` | per-minute limits for anonymous / members / API keys / downloads / auth |
| `Email` | `Console` (log) or `Smtp` |
| `RepositoryImport` | optional `GitHubToken`, `GitLabToken` |
| `Admin` | `Email`, `Username`, `InitialPassword` |
| `Seed` | `LlmProjectsPath`, `DemoMemberPassword` |

### AI providers

`Infrastructure/Ai/ILlmProvider.cs` is the single abstraction (embeddings + chat), the port of the CL-AI `LlmFactory` pattern.
`OllamaLlmProvider` talks to a local/containers Ollama (`bge-m3` embeddings, `qwen3.8` chat); `OpenAiCompatibleLlmProvider` talks to
any OpenAI-compatible endpoint with an API key. `Ai:Provider` selects the default; `Ai:EmbeddingProvider` / `Ai:ChatProvider` can
split them (e.g. Ollama embeddings locally, OpenAI chat in production). Vectors are stored as `vector(1024)`; OpenAI
`text-embedding-3-*` models are requested with `dimensions=1024`. Changing the embedding model → Admin › Jobs › `reembed_all`.

### Search

PostgreSQL only: a weighted generated `tsvector` (name A, short description B, tags + category C, long description D) with a GIN
index and a trigram index on the name for keyword search; pgvector HNSW (cosine) for semantic search; both legs are pre-filtered
by category subtree / platform / license / model / rating / tags and fused with reciprocal rank fusion (`Services/Search/SearchService.cs`).
The same vector query powers the duplicate warning in the upload wizard and the agent verdict.

## Layout

```
AiAlreadyDidIt.Api/
  Contracts/<Domain>/      DTOs and request models (Common: envelope, paging, ApiException, ValidationBag)
  Controllers/V1/<Domain>/ Account, Site (config, savings, files), Catalog, Apps (download, ratings, report), MyApps (upload wizard),
                           Dashboard, Requests (wanted board), Agent, Seo, Admin/* (moderation, reports, requests, apps, catalog lists,
                           users, api keys, sessions, identity, featured, banners, settings, stats, audit, jobs, health)
  Data/                    AadiDbContext, Configurations/, Migrations/, Seed/ (reference data), DbSeeder (demo)
  Entities/                Identity, Reference, Catalog, Engagement, Platform
  Infrastructure/          Options, CurrentUser, Middleware, Ai/ (providers), Auth/ (JWT, API keys, role-action filter), Storage/ (S3, images),
                           Scanning/ (ClamAV), Import/ (GitHub, GitLab, license detector, source analyzer), Jobs/ (durable queue), Email/
  Services/<Domain>/       one service per controller (auto-registered by the "*Service" suffix); Jobs/ holds the IJobHandler implementations
AiAlreadyDidIt.Mcp/        StoreTools (MCP tools), AadiApiClient, Program (http | --stdio)
```

## Migrations

```bash
cd AiAlreadyDidIt.Api
dotnet ef migrations add <Name> -o Data/Migrations
dotnet ef database update
```
