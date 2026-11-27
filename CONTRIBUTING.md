# Contributing

Thanks for your interest. This is a small personal project, published so others can run it, learn from it and
experiment with it. Issues and pull requests are welcome; for anything larger than a bug fix, please open an issue
first so we can agree on the approach.

## Development setup

Requirements: Docker with Compose. For running tests and builds outside containers: .NET SDK 10
(see `global.json`) and Node.js 25 (what CI uses).

```bash
cp .env.example .env
docker compose up -d
```

See the [README](README.md#quick-start-development) for URLs and the first admin login.

## Before you open a pull request

Run the same checks CI runs:

```bash
cd src/backend && dotnet test   # uses a cms_test database on the dev PostgreSQL (docker compose up -d db)
cd src/frontend/web && npm ci && npm run lint && npm run typecheck && npm run build
cd src/frontend/admin && npm ci && npm run lint && npm run typecheck && npm run build
```

- Follow the conventions in [docs/](docs/): layering and naming in
  [backend-code-structure.md](docs/backend-code-structure.md), raw SQL through Npgsql at runtime and EF Core for
  migrations only ([database-layer.md](docs/database-layer.md)), and the URL and response format in
  [api-design.md](docs/api-design.md).
- Add or update tests for behaviour changes. Security fixes need a regression test.
- Schema changes go through an EF Core migration in `src/backend/migrations`; never edit an existing migration.
- Never commit secrets, real `.env` files, certificates or private keys. CI runs gitleaks.
- Keep pull requests focused; one topic per PR. Write commit messages in the imperative
  ("Add series ordering"), in English.

## Dependency updates

There is no Dependabot. Dependencies are updated by hand in batches (one PR per ecosystem). If you bump a
package, run the full check list above and mention any breaking changes in the PR.

## Task tracking

The maintainer plans work in a private [Backlog.md](https://github.com/MrLesk/Backlog.md) board that is not part of
this repository. For bugs, ideas and proposals, open a GitHub issue.

## AI-assisted contributions

Much of this project was written with AI coding agents (see [docs/ai-workflow.md](docs/ai-workflow.md)).
AI-assisted pull requests are welcome on the same terms as any other: you are responsible for the change, you have
read and understood it, and it passes the checks above.

## License

By contributing you agree that your contribution is licensed under the [MIT License](LICENSE).
