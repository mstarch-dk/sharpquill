# How this project was built with AI agents

Sharpquill was built by one developer working with AI coding agents, mostly Claude Code and, for some stretches,
OpenAI Codex. The developer chose what to build, wrote or approved each task, reviewed the results and ran the
production sites. The agents wrote most of the code, tests, migrations and docs.

This page describes the setup, the loop that ended up working, and, most usefully, what the agents got wrong and
how it was caught.

## The setup

| Piece | What it does |
| --- | --- |
| [`CLAUDE.md`](../CLAUDE.md) / [`AGENTS.md`](../AGENTS.md) | Project instructions loaded by Claude Code and Codex at the start of every session: stack, conventions, which docs to read, required verification commands, guardrails (no production access, no secrets, no destructive git). |
| [`docs/`](.) | The architecture docs the instructions point to. Keeping conventions in docs rather than in prompts is what made the code consistent across hundreds of sessions. |
| [Backlog.md](https://github.com/MrLesk/Backlog.md) | Every piece of work is a Markdown task with a description, acceptance criteria, definition of done, implementation plan, notes and final summary. The board lives in a separate private repository (it records details of the maintainer's own servers), checked out next to this one. [docs/examples/sample-task.md](examples/sample-task.md) shows one real task. |
| Backlog.md MCP server | Exposes the board to the agents as MCP tools (`task_view`, `task_edit`, ...), so they read and update tasks directly instead of editing Markdown by hand. Configured per machine and pinned to the board's checkout (`backlog mcp start --cwd ...`), not in the repository. |
| [`.claude/settings.json`](../.claude/settings.json) | A shared allow-list of read-only and build/test commands, so routine checks don't need a permission prompt but anything else does. |
| CI ([`.github/workflows/ci.yml`](../.github/workflows/ci.yml)) | Backend tests against real PostgreSQL, frontend lint/typecheck, Docker image builds, gitleaks, NuGet/npm vulnerability gates. The agents' "done" is not trusted until CI agrees. |

## The loop

1. **Task.** A task is created with a concrete description and checkable acceptance criteria ("admin login returns
   404 on the public host"), not just a goal ("improve security").
2. **Plan.** Before writing code the agent reads the task and the relevant docs, writes an implementation plan into
   the task, and sets it to *In Progress*. For bigger changes the human reads the plan first; this is the cheapest
   point to catch a wrong approach.
3. **Implement.** The agent works on a branch, in small commits.
4. **Verify against the definition of done.** The agent runs the tests and builds, and checks each acceptance
   criterion with evidence: a test, a `curl` against a running stack, a grep. It ticks only what it actually
   verified, and writes down what it could *not* verify.
5. **Close.** A final summary goes into the task (what changed, which files, decisions, open points), the task is
   set to *Done*, and the human decides whether to commit or open a pull request.

The task files are the project's memory. A new session, possibly with a different agent, starts from the task and
the docs rather than from a conversation that no longer exists.

## What worked well

- **Conventions written down once.** "Npgsql with raw SQL at runtime, EF Core for migrations only" and the
  `{ "data": ... }` / `{ "error": ... }` response shape are stated in the docs, and the agents followed them
  consistently across about 80 tasks. When a convention lived only in someone's head, the agents invented their own.
- **Acceptance criteria as the contract.** Tasks with criteria that could be tested produced work that could be
  checked. Vague tasks produced plausible-looking work that was hard to judge.
- **Integration tests against a real database.** About 270 backend tests run against PostgreSQL, which caught
  mistakes in hand-written SQL and mapping that mocks would have hidden.
- **Rehearsals before production.** Risky changes (the security release, the switch to published images) were
  rehearsed on scratch stacks restored from copies of real backups, with row counts and file checksums compared
  before and after.
- **Adversarial review by a separate agent run.** Asking an agent to *attack* the project found far more than asking
  the agent that wrote the code whether it was correct (see below).

## What went wrong

### Security bugs the agents introduced

Before publishing, an agent was asked to do an adversarial audit of the code, the deployment configuration and the
git history, as if an attacker were reading it. The most serious findings were then checked by hand. It found real,
exploitable problems, all introduced during normal feature work and all missed by the agent that wrote the code:

| Bug | How it happened | Fix |
| --- | --- | --- |
| **Certificate login could be faked through the public site.** | Admin login by client certificate trusted an `X-Client-Cert-Fingerprint` header set by the proxy. The proxy set it on the admin host, but the public host passed the API through without stripping the header, so anyone could send it. The fingerprint is not a secret. | The proxy strips the header on every route and only sets it from a verified certificate; the admin API is not routed on the public host at all. |
| **The admin API was reachable on the public hostname.** | The IP allowlist and mTLS were configured for the admin hostname only, while the public hostname forwarded all of `/api/*`. | `/api/admin/*` answers 404 on the public host; regression checks in the deploy rehearsal. |
| **Refreshing a token made any author an admin.** | The refresh-token query did not select the author's role, and the `Role` property defaulted to `"admin"`. Every refreshed token therefore carried the admin role. Tests passed because test authors were admins. | Role selected in the query, default changed to the least-privileged role, regression test for a `user`-role refresh. |
| **Zip slip in content-package import.** | Archive entry names were combined with the uploads path without checking that the result stayed inside it, so a crafted package could write files anywhere the API could. No limit on decompressed size either. | Canonical-path prefix check, size and entry-count limits, tests with malicious archives. |
| **Uploads trusted the client's content type.** | The client-supplied MIME type and file extension were accepted, SVG was allowed, and uploads were served from the same origin as the admin panel, a stored-XSS path. | Type detected from file content, extension allowlist, no SVG, `nosniff`. |
| **A known admin password in every database.** | The first migration seeded an `admin@example.com` account with a well-known example password hash "to change in production". | Seed removed; the first admin is created by `db-init` from environment variables or a random printed password. |
| **SSRF in link previews**, **unsanitized HTML**, **no rate limits on login**, **a placeholder JWT secret accepted at startup**, **one JWT audience for admins and readers**, **a "login disabled" setting enforced only in the UI**. | Each was the simplest thing that made the feature work. | Each got a task, a fix and tests. |

The common pattern: **the agents implemented the happy path of a feature carefully, but did not think about the
system around it.** The certificate-header bug sat in the space between the API code, which was correct for the
trust model it assumed, and the proxy configuration, which broke that assumption. No single file was wrong.

### Process mistakes

- **Skipped bookkeeping.** Agents sometimes finished the code and reported success without writing the plan,
  ticking criteria or writing the summary into the task, or wrote them in the chat instead of through the backlog
  tools. The project instructions now require a `task_view` after every write to prove it was saved.
- **"Done" without proof.** Early on, criteria were ticked because the code *looked* right. The rule now is: tick
  only with evidence, and say explicitly what was not verified. Several tasks stay *In Progress* for exactly that
  reason.
- **CI that never ran.** CI was configured for pull requests while all work was pushed straight to `main`, and the
  typecheck step was allowed to fail. The agents never noticed, because nothing told them. CI now runs on every push
  and pull request and blocks on failures; a deliberately broken pull request proved that it fails.
- **Secrets and personal infrastructure in the repository.** Agents happily committed what they were working on:
  a production proxy configuration with real hostnames and an IP range, a production database command in the
  instructions file, a local settings file with a development token, build output. None of it was a live secret, but
  it is why the public repository was started with a fresh, curated history.
- **Docs that drifted.** Instruction files and docs described an older proxy, an older startup command and an
  incomplete list of environment variables. Agents trust the docs, so stale docs led to wrong changes. Docs are now
  part of a task's definition of done.

## Lessons

1. Write conventions and guardrails into files the agent always reads; keep them short and current.
2. Make acceptance criteria testable, and require evidence for each one.
3. Have a different agent run, with a different brief, attack the result. Security problems hid between
   components, where no single author looked.
4. Let CI, not the agent, decide when something passes.
5. Keep deployment details and secrets out of the code repository from day one; here they now live in a separate,
   private operations repository.
