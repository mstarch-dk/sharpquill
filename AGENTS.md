# Codex Agent Guide for Sharpquill

## Purpose
This repository contains Sharpquill, a custom CMS with:
- ASP.NET Core API (`src/backend`)
- Nuxt admin app (`src/frontend/admin`)
- Nuxt public web app (`src/frontend/web`)

## Mandatory workflow
- If the Backlog.md MCP server (`backlog`) is connected (the maintainer's private task board, a separate
  repository), read its document "Agent workflow" (doc-3) before working on tasks and follow it.
- Read architecture docs before non-trivial changes:
  - `docs/project-structure.md`
  - `docs/backend-code-structure.md`
  - `docs/database-layer.md`
  - `docs/api-design.md`
  - `docs/frontend-uis.md`

## Source-of-truth conventions
- Runtime DB access: Npgsql + raw SQL (no EF runtime queries).
- EF Core usage: migrations only in `src/backend/migrations`.
- API routes:
  - Public: `/api/v1/*`
  - Admin: `/api/admin/*`
- API response shape: `{"data": ...}` and structured `{"error": ...}`.

## Required verification before finishing
- Backend: `cd src/backend && dotnet test`
- Web frontend: `cd src/frontend/web && npm run lint && npm run typecheck && npm test && npm run build`
- Admin frontend: `cd src/frontend/admin && npm run lint && npm run typecheck && npm test && npm run build`

## Guardrails
- Do not access or modify production systems unless explicitly requested.
- Never use destructive git commands unless explicitly requested.
- Do not commit secrets, certificates, private keys, or real env files.
- Keep changes scoped to the user request; avoid opportunistic refactors.

## Quick orientation
- API composition root: `src/backend/api/Program.cs`
- Test project: `src/backend/api.Tests`
- Admin Nuxt config: `src/frontend/admin/nuxt.config.ts`
- Web Nuxt config: `src/frontend/web/nuxt.config.ts`
