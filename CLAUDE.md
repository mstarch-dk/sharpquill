# Sharpquill - Design Decisions & Development Guide

## Project overview
This is Sharpquill, a self-built CMS / blogging system (C# backend, Nuxt frontends).
The purpose of the system is to allow content creators to write engaging articles and to have them published and made available for viewers on a website.

Naming: the brand is written "Sharpquill" in everything people read (UI, docs, logo); code identifiers keep
`SharpQuill` (`SharpQuill.*` namespaces, `SharpQuillDbContext`, `SharpQuill.sln`); lower-case names (`sharpquill-*`
images, npm packages) stay lower-case. Logo, icons and the Ink teal palette live in `assets/brand/`.

## Project management
If the Backlog.md MCP server (`backlog`) is connected, it serves the maintainer's private task board, which lives in a
separate repository. Before working on tasks, read its document "Agent workflow" (`document_view` doc-3) and follow it.
Without that server there is no task board.

## Stack Overview

- **Frontend**: Nuxt 4 (4.6) + TypeScript (two apps: public website with SSR + admin panel as SPA)
- **Backend**: C# ASP.NET Core 10 Web API
- **Database**: PostgreSQL 17
- **Data Access**: Npgsql for queries, EF Core Code First for migrations only
- **Authentication**: JWT with rotating refresh tokens (separate audiences for admin authors and website readers), optional client-certificate login for the admin
- **Reverse proxy (production)**: Caddy (TLS, routing, security headers) - reference config in `deploy/Caddyfile.example`
- **Deployment**: CI publishes `sharpquill-{api,web,admin,db-init}` images to GHCR; a site runs them with `deploy/compose.yml` (see docs/deployment.md). This repo contains no deployment of a specific site.

## Docs references
<CRITICAL_INSTRUCTION>
REFERENCE these docs for more details about:
- Project structure -> docs/project-structure.md
- Backend project details (C# Naming conventions, Project dependencies, Database design, Data Access Pattern, Repository pattern, Service layer) -> docs/backend-code-structure.md
- Database layer principles (Postgres, table layout, design conventions) -> docs/database-layer.md
- API design (URL structure, Response format, Error Format, Authentication) -> docs/api-design.md
- Frontend UI details (Nuxt admin and web frontends) -> docs/frontend-uis.md
</CRITICAL_INSTRUCTION>


## Content Storage

- Articles stored as Markdown in database
- Rendered to HTML on frontend (SSR)
- Media files stored on filesystem with Docker volumes

## Development Commands

### Starting Development Environment

```bash
# First time only: create the dev config (the single source of config for docker compose)
cp .env.example .env

# Start all services (db, api, web, admin, umami, pgadmin; ports bind to 127.0.0.1)
docker compose up -d

# First admin login: ADMIN_EMAIL / ADMIN_PASSWORD from .env, or the random password printed by
#   docker compose logs db-init

# View logs
docker compose logs -f api

# Rebuild after code changes
docker compose up -d --build api
```

### Running Tests

```bash
# Needs the dev database running (docker compose up -d db)
cd src/backend
dotnet test
```

The tests use a separate `cms_test` database (created and truncated by the fixture, never `POSTGRES_DB`). They connect
via `ConnectionStrings__DefaultConnection` if set (CI), otherwise via `DB_PORT` / `POSTGRES_USER` /
`POSTGRES_PASSWORD` from the repo `.env` (see `api.Tests/Fixtures/TestDatabase.cs`). All test hosts share one
connection pool capped at 30 connections. A test that starts its own host with `WithWebHostBuilder` must dispose it.

### Coverage

CI collects coverage on every run, prints a summary in the job summary, uploads the HTML reports as artifacts
(`coverage-backend`, `coverage-web`, `coverage-admin`) and fails below the minimums: backend `MIN_LINES` /
`MIN_BRANCHES` in the "Coverage gate" step of `.github/workflows/ci.yml`, frontend `coverage.thresholds` in each
app's `vitest.config.ts`. They are a ratchet: raise them when coverage grows, never lower them to make a PR pass.

```bash
# Backend (ReportGenerator is a local dotnet tool, see src/backend/dotnet-tools.json)
cd src/backend
dotnet test --settings api.Tests/coverlet.runsettings --collect "XPlat Code Coverage" --results-directory TestResults
dotnet tool restore
dotnet reportgenerator -reports:"TestResults/*/coverage.cobertura.xml" -targetdir:coverage-report -reporttypes:"Html;TextSummary"
cat coverage-report/Summary.txt          # open coverage-report/index.html for the full report
rm -rf TestResults                       # each run adds a new folder; clear it before the next run

# Frontend (same in src/frontend/admin): text summary in the terminal, HTML in coverage/index.html
cd src/frontend/web
npm run test:coverage
```

### Frontend Validation

```bash
cd src/frontend/web
npm run lint
npm run typecheck
npm test
npm run build

cd ../admin
npm run lint
npm run typecheck
npm test
npm run build
```

## Environment Variables

Dev values live in `.env` (from `.env.example`); production values in `deploy/.env` (from `deploy/.env.example`),
which `deploy/compose.yml` maps onto the variables below.

### Backend (API)

- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `Jwt__Secret` - JWT signing secret (min 32 chars; the API refuses to start with a placeholder or short secret)
- `Jwt__Issuer` / `Jwt__Audience` - JWT issuer and base audience URL (admin and website tokens get distinct audiences derived from it)
- `Jwt__AccessTokenExpirationMinutes` / `Jwt__RefreshTokenExpirationDays` - token lifetimes (default 15 min / 7 days)
- `Cors__AdminOrigins__0` / `Cors__PublicOrigins__0` - browser origins allowed for the admin API and the public API (`Cors__Origins__N` is the legacy shared list)
- `Email__Host`, `Email__Port`, `Email__Username`, `Email__Password`, `Email__FromAddress`, `Email__FromName` - SMTP for the contact form (contact form answers 503 until the contact email setting is set and Host, Username, Password and FromAddress are all non-empty)
- `Uploads__Path` / `Uploads__UrlBase` - where uploaded media is stored and served from (defaults `uploads` and `/uploads`)

### db-init (first run)

- `ADMIN_EMAIL` / `ADMIN_PASSWORD` - first admin, created only while the authors table is empty (password min 12 chars; if unset a random one is printed once in the db-init logs)

### Frontend

Read at container start, so one image serves every site (nothing is baked in at build time):

- `NUXT_PUBLIC_API_BASE` - API base URL used by the browser (e.g., http://localhost:5000)
- `NUXT_API_BASE_INTERNAL` - web only: API base URL used for SSR requests inside the Docker network (e.g., http://api:5000)
- `NUXT_PUBLIC_UMAMI_HOST` / `NUXT_PUBLIC_UMAMI_WEBSITE_ID` - web only: Umami script host and website id (analytics disabled if empty)
- `NUXT_UPLOADS_PROXY` - where `/uploads/**` is proxied to (e.g., http://api:5000). Admin: always. Web: optional, set in dev only; unset = no proxy, because in production Caddy routes `/uploads/*` to the API

## Docker Services

| Service | Port   | Description              |
| ------- | ------ | ------------------------ |
| db      | 5432   | PostgreSQL 17            |
| api     | 5000   | C# Web API (dotnet watch hot reload from `src/backend`) |
| web     | 3000   | Public website           |
| admin   | 3001   | Admin panel              |
| umami   | 3003   | Analytics dashboard      |
| pgadmin | 5050   | Database UI (dev only)   |

All dev ports bind to 127.0.0.1 and are overridable in `.env` (`WEB_PORT`, `ADMIN_PORT`, `API_PORT`, ...).

## Production Access

- Never access a production server, database or deployment unless the user explicitly asks for it in that session.
- Server details, real hostnames and secrets do not belong in this repository; operators keep them in their own
  (private) ops setup.
