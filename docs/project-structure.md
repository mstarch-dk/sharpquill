## Project Structure

> See also: [backend-code-structure.md](backend-code-structure.md) for naming conventions and service layer, [database-layer.md](database-layer.md) for data access patterns, [frontend-uis.md](frontend-uis.md) for frontend conventions, [api-design.md](api-design.md) for API endpoints

```
(repository root)
├── assets/brand/            # Sharpquill logo, icons, favicon PNGs and the Ink teal palette (README inside)
├── src/
│   ├── backend/
│   │   ├── SharpQuill.sln
│   │   ├── api/             # ASP.NET Core Web API namespace: SharpQuill.Api
│   │   │   ├── Controllers/     # Admin/ and V1/ controllers
│   │   │   └── Infrastructure/  # Auth (schemes, policies), Filters, Middleware, Models
│   │   ├── core/            # Domain entities, interfaces, enums namespace: SharpQuill.Core
│   │   │   ├── Articles/        # Article, ArticleContent, ArticleTag, ArticleStatus, ContentType, etc.
│   │   │   ├── Authors/         # Author, IAuthorRepository, IAdminAuthorRepository
│   │   │   ├── Auth/            # RefreshToken, IRefreshTokenRepository
│   │   │   ├── Categories/      # Category, ICategoryRepository, IAdminCategoryRepository
│   │   │   ├── Media/           # Media, IMediaRepository
│   │   │   ├── Navigations/     # Navigation, NavigationItem, NavigationItemType
│   │   │   ├── Series/          # Series, ISeriesRepository, IAdminSeriesRepository
│   │   │   ├── Settings/        # SiteSettings, ISiteSettingsRepository
│   │   │   ├── Tags/            # Tag, ITagRepository, IAdminTagRepository
│   │   │   └── Common/          # PaginationParams
│   │   ├── application/     # Services, DTOs, repositories namespace: SharpQuill.Application
│   │   │   └── {Domain}/        # Each domain has DTOs/, Services/, Repositories/, Validators/
│   │   └── migrations/      # EF Core DbContext + migrations only namespace: SharpQuill.Migrations
│   │   ├── db-init/         # Console app run by the db-init container: applies migrations, creates the first admin
│   │   └── api.Tests/       # xUnit integration tests against a real PostgreSQL
│   └── frontend/
│       ├── web/                 # Public Nuxt website (port 3000); test/unit/ + test/nuxt/ = Vitest tests
│       ├── admin/               # CMS admin panel (port 3001); test/unit/ + test/nuxt/ = Vitest tests
│       ├── layers/shared/       # Nuxt layer both apps extend: shared components, utils, types, block registry
│       └── themes/              # Theme definitions shared by both apps
├── docker/
│   ├── api/                     # API docker setup (Dockerfile = dev, Dockerfile.prod = published image)
│   └── db-init/                 # Docker file for db migration init container.
├── deploy/                      # Generic single-site production reference (compose, Caddyfile.example, .env.example)
├── scripts/                     # backup.sh / restore.sh for deploy/, certs/ for admin client certificates
├── docs/                        # Architecture, API, deployment, privacy and AI-workflow docs
├── .github/workflows/           # ci.yml (tests, lint, image builds, scans), publish-images.yml (GHCR)
├── .claude/                     # Shared Claude Code settings
├── CLAUDE.md, AGENTS.md         # Instructions for AI coding agents
├── .env.example                 # Dev configuration (copy to .env)
└── docker-compose.yml           # Development stack
```