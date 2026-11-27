# Security policy

## Reporting a vulnerability

Please **do not open a public issue** for security problems.

Report them privately through GitHub: open the repository's **Security** tab and choose
**Report a vulnerability** (GitHub private vulnerability reporting). Include:

- what is affected (endpoint, component, file) and the version or commit you tested,
- steps to reproduce or a proof of concept,
- the impact you expect (for example: privilege escalation, data disclosure).

You can expect an acknowledgement within 7 days. Fixes are released as a new image version; the advisory is
published once a fix is available. This is a one-person project maintained in spare time, so there is no bug bounty.

## Supported versions

Only the latest release (and `main`) receives security fixes. Operators should run a recent
`ghcr.io/<owner>/sharpquill-*` image tag and follow the upgrade steps in [docs/deployment.md](docs/deployment.md).

## Scope and trust model

Things that are **by design** and not vulnerabilities on their own:

- **Authors are trusted.** Anyone with an admin-panel account can publish content. Rendered HTML is sanitized,
  but authors can still embed links, images and layouts of their choice.
- **The reverse proxy is part of the security boundary.** The API trusts the `X-Client-Cert-Fingerprint` header for
  certificate login, so the API port must only be reachable through a proxy that strips or overwrites that header
  (the reference [Caddyfile](deploy/Caddyfile.example) does). See [docs/admin-security.md](docs/admin-security.md).
- **The admin panel should not be exposed to the whole internet.** The reference setup serves it on its own
  hostname with optional mTLS and an IP allowlist.

Reports that show a way around these boundaries (for example reaching the admin API through the public host) are
very welcome.
