# Deploying a site

This guide takes you from a fresh Linux VM to one running Sharpquill site (public website, admin panel, API, database and
analytics) served over HTTPS, using the published Docker images and the reference files in [`deploy/`](../deploy).

One stack is one site. To host several sites, run one stack per site (see [Several sites on one server](#several-sites-on-one-server)).

## What you get

| Service    | Image                                   | Purpose                                              |
| ---------- | --------------------------------------- | ---------------------------------------------------- |
| `db`       | `postgres:17`                           | Database (`cms`, plus `umami` for analytics)          |
| `db-init`  | `<prefix>/sharpquill-db-init:<version>`        | Applies migrations, creates the first admin (exits)   |
| `api`      | `<prefix>/sharpquill-api:<version>`            | C# Web API, owns the uploads volume                   |
| `web`      | `<prefix>/sharpquill-web:<version>`            | Public website (Nuxt SSR)                             |
| `admin`    | `<prefix>/sharpquill-admin:<version>`          | Admin panel                                           |
| `umami`    | `ghcr.io/umami-software/umami`          | Analytics                                             |
| `caddy`    | `caddy:2` (optional, profile `caddy`)   | HTTPS reverse proxy                                   |

The images are built once and configured at start-up through environment variables, so the same image serves every site.
Nothing in them is specific to a domain.

## 1. Prerequisites

- A VM or server running Linux with a public IP address (any distribution that runs Docker; 2 vCPU / 2 GB RAM is enough to start).
- Two DNS names pointing at the server, plus an optional third:
  - `example.com` (the public site) and `www.example.com` (redirects to it)
  - `admin.example.com` (the admin panel; must be a different hostname from the public site)
  - `analytics.example.com` (optional, the Umami dashboard)
- Ports 80 and 443 reachable from the internet (Let's Encrypt validation uses them). Keep everything else closed
  in the firewall: the application ports only listen on `127.0.0.1`.
- [Docker Engine with the Compose plugin](https://docs.docker.com/engine/install/) (`docker compose version` works).
- A version to run. The project publishes the images `ghcr.io/mstarch-dk/sharpquill-{api,web,admin,db-init}` as **public**
  packages, so the server pulls them anonymously (no `docker login`). Tags: `sha-<short>` for every push to `main`,
  and `X.Y.Z` / `X.Y` for every version tag `vX.Y.Z`. The `latest` tag only exists once the first release has been
  tagged. Pick a tag from the [package page](https://github.com/users/mstarch-dk/packages/container/package/sharpquill-api)
  and use the same tag for all four images.
  If you publish your own copy, set `SHARPQUILL_IMAGE_PREFIX` to your namespace. If your packages are private, log the server
  in first with a token that only has the `read:packages` scope:
  `echo <token> | docker login ghcr.io -u <github-user> --password-stdin`.

## 2. Get the deployment files

You only need the `deploy/` directory and the `scripts/` directory (backup and restore):

```bash
sudo mkdir -p /srv/cms && sudo chown "$USER" /srv/cms
git clone --depth 1 https://github.com/mstarch-dk/sharpquill.git /srv/cms/src
cp -r /srv/cms/src/deploy /srv/cms/deploy
cp -r /srv/cms/src/scripts /srv/cms/scripts
cd /srv/cms/deploy
```

Keeping a copy outside the clone means a `git pull` never overwrites your `.env` and `Caddyfile`.

## 3. Configure

```bash
cp .env.example .env
chmod 600 .env
cp Caddyfile.example Caddyfile    # only if you use the bundled Caddy
```

Edit `.env`. Generate each secret with `openssl rand -hex 32`. Use hex rather than base64: the database password is
also placed inside Umami's connection URL, where base64 characters such as `/` and `+` break it.

| Variable | What to set |
| -------- | ----------- |
| `SHARPQUILL_IMAGE_PREFIX`, `SHARPQUILL_VERSION` | Registry namespace and the version to run (required). **Pin a version** (for example `1.2.3`, or `sha-<short>` before the first release) rather than `latest`. |
| `DOMAIN`, `ADMIN_DOMAIN` | Public and admin hostnames (no scheme). |
| `ANALYTICS_DOMAIN` | Optional Umami dashboard hostname. |
| `POSTGRES_PASSWORD` | Strong database password. Set it before the first start. |
| `JWT_SECRET` | At least 32 random characters. Changing it signs everyone out. |
| `ADMIN_EMAIL`, `ADMIN_PASSWORD` | First admin account. The password needs at least 12 characters; leave it empty to get a random one (see below). |
| `UMAMI_APP_SECRET` | Random string for Umami. |
| `EMAIL_*` | SMTP settings for the contact form (optional). |

### CORS

The browser calls the API directly, so the API must know which origins to trust. `deploy/compose.yml` derives them
from your domains and passes them to the API as:

```
Cors__AdminOrigins__0=https://<ADMIN_DOMAIN>     # credentialed calls to /api/admin/* only
Cors__PublicOrigins__0=https://<DOMAIN>          # everything else
```

If you write your own compose file or Kubernetes manifest, set these two variables yourself (ASP.NET Core array
syntax, add `__1`, `__2` for more origins). If they are missing the API falls back to the development defaults, which
point at `localhost`, and browser requests from your real domains are rejected. Background: [security-headers.md](security-headers.md).

### What the web and admin containers are told

`compose.yml` sets these at runtime (you normally do not change them):

| Variable | Value | Meaning |
| -------- | ----- | ------- |
| `NUXT_PUBLIC_API_BASE` (web) | `https://<DOMAIN>` | API address used by visitors' browsers |
| `NUXT_API_BASE_INTERNAL` (web) | `http://api:5000` | API address for server-side rendering, inside Docker |
| `NUXT_PUBLIC_UMAMI_HOST` / `NUXT_PUBLIC_UMAMI_WEBSITE_ID` (web) | `https://<DOMAIN>/umami` / `UMAMI_WEBSITE_ID` | Analytics script; disabled while the id is empty |
| `NUXT_PUBLIC_API_BASE` (admin) | empty | Admin calls the API on its own hostname |
| `NUXT_UPLOADS_PROXY` (admin) | `http://api:5000` | Where the admin app proxies `/uploads/**` |

## 4. Start

With the bundled Caddy (obtains and renews certificates automatically):

```bash
docker compose --profile caddy up -d
```

Without it (you run your own proxy on the host, pointing at `127.0.0.1:5000/3000/3001/3002`):

```bash
docker compose up -d
```

Check that everything is healthy (the first start takes a minute while images are pulled and migrations run):

```bash
docker compose ps
docker compose logs db-init
```

### First login

`db-init` creates the first admin from `ADMIN_EMAIL` / `ADMIN_PASSWORD` while the `authors` table is empty. If you left
`ADMIN_PASSWORD` empty, a random password was generated and printed once:

```bash
docker compose logs db-init | grep -i password
```

Open `https://<ADMIN_DOMAIN>`, sign in, change the password, and then remove `ADMIN_PASSWORD` from `.env`.

### Analytics

Open `https://<ANALYTICS_DOMAIN>` (default login `admin` / `umami`, change it immediately), add your website, copy its id
into `UMAMI_WEBSITE_ID` in `.env` and run `docker compose up -d` again. The public site then loads the tracker from
`https://<DOMAIN>/umami/script.js`; only the tracker script and its collection endpoint are public.

## 5. Reverse proxy notes

`Caddyfile.example` implements these rules, which you must keep if you use another proxy:

- **The admin API is never served on the public hostname.** `/api/admin/*` answers 404 on `DOMAIN`; it is only routed on `ADMIN_DOMAIN`.
- **`X-Client-Cert-Fingerprint` is stripped** on every request except where it is set from a verified client certificate.
- **Security headers** (HSTS, `nosniff`, referrer policy, permissions policy, and a CSP plus `X-Frame-Options: DENY` on the admin host), see [security-headers.md](security-headers.md).
- **Optional hardening for the admin host**, both commented out in the example:
  - an IP allowlist (`import allowlist` with `ADMIN_ALLOWED_IPS` in `.env`), and
  - mutual TLS with client certificates for passwordless admin login, see [admin-security.md](admin-security.md) and
    `scripts/certs/generate-ca.sh` / `generate-client-cert.sh`. Put the CA certificate at `deploy/certs/ca.crt` and uncomment the `tls { client_auth ... }` block.
- Nothing but ports 80 and 443 should be reachable from outside. The database is not published at all; use
  `docker compose exec db psql -U cms_user cms` for ad-hoc access.

## 6. Backups

`scripts/backup.sh` backs up, in one timestamped directory: the `cms` database, the `umami` database, the cluster
globals (roles) and the uploads volume. It verifies every artifact (dump listing, archive test, SHA-256 checksums,
per-table row counts) before the backup appears under its final name, and exits non-zero on any failure.

```bash
DEPLOY_DIR=/srv/cms/deploy BACKUP_DIR=/var/backups/cms /srv/cms/scripts/backup.sh
```

Schedule it and watch the exit status or log (daily at 03:00):

```
0 3 * * * DEPLOY_DIR=/srv/cms/deploy BACKUP_DIR=/var/backups/cms /srv/cms/scripts/backup.sh >> /var/log/cms-backup.log 2>&1
```

Old backups are removed after `RETENTION_DAYS` (default 14). **Copy backups off the server** (object storage, another
host): a backup that lives on the same disk does not protect you from losing the machine.

## 7. Restore, and the restore drill

`scripts/restore.sh <backup-directory>` replaces the databases and the uploads of the stack in `DEPLOY_DIR`. It checks
the checksums first, restores, and then compares the row count of every table with the counts recorded at backup time.
A mismatch makes it exit non-zero.

Restoring on a new server:

```bash
cd /srv/cms/deploy
# 1. Put the SAME .env you used for the backed-up stack in place (database user and password must match the
#    roles in the backup; JWT_SECRET must match or users are signed out).
# 2. Start only the database and create (not start) the api container:
docker compose up -d db
docker compose create api
# 3. Restore (asks for confirmation; add --yes to skip it):
DEPLOY_DIR=/srv/cms/deploy /srv/cms/scripts/restore.sh /var/backups/cms/20260101_030000
```

The script starts the whole stack afterwards. Check `docker compose ps`, sign in to the admin panel and open a few articles.

### Drill: prove your backups work

Run this once after the first deployment and again after changing the setup. It never touches the live stack.

1. Make a backup of the live stack (section 6).
2. On a scratch machine, or on the same host with different ports, create a second stack from the same files
   with its own project name and ports (this is what the verification of this guide used):
   ```bash
   cp -r /srv/cms/deploy /srv/cms/drill && cd /srv/cms/drill
   sed -i 's/^COMPOSE_PROJECT_NAME=.*/COMPOSE_PROJECT_NAME=cmsdrill/' .env
   printf 'API_PORT=5951\nWEB_PORT=3951\nADMIN_PORT=3952\nUMAMI_PORT=3953\n' >> .env
   docker compose up -d db && docker compose create api
   ```
3. Restore into it: `DEPLOY_DIR=/srv/cms/drill /srv/cms/scripts/restore.sh <backup-dir> --yes`.
   The script prints `row counts match (N tables)` for both databases.
4. Compare independently: the same articles through `http://127.0.0.1:5951/api/v1/articles`, a login with an existing
   admin on `127.0.0.1:5951`, and the uploads checksum on both stacks:
   ```bash
   docker compose exec -T api sh -c 'cd /app/uploads && find . -type f | sort | xargs sha256sum | sha256sum'
   ```
5. Remove the drill: `docker compose down -v` in `/srv/cms/drill`.

## 8. Upgrades

```bash
cd /srv/cms/deploy
# set SHARPQUILL_VERSION in .env to the new release, then
docker compose --profile caddy pull
docker compose --profile caddy up -d
```

`db-init` runs the new migrations before the API starts. Take a backup first. To roll back, set the previous
`SHARPQUILL_VERSION` again (restore the backup if the release included a migration that cannot be reversed).

## Several sites on one server

Give every site its own directory, `.env`, `COMPOSE_PROJECT_NAME` and host ports (`API_PORT`, `WEB_PORT`, `ADMIN_PORT`,
`UMAMI_PORT`), and run a single Caddy on the host for all of them (leave the `caddy` profile off) with one block per
site, upstreams `127.0.0.1:<port>`. The bundled Caddy owns ports 80 and 443, so only one stack can use it.

## Troubleshooting

| Symptom | Likely cause |
| ------- | ------------ |
| Admin panel loads but every request fails in the browser console with a CORS error | `ADMIN_DOMAIN` in `.env` does not match the hostname you browse to (the `Cors__AdminOrigins__0` value must equal the page's origin, including `https://`). |
| Public site shows no data but the API works | `DOMAIN` is wrong, so the browser calls an unreachable API address. |
| `docker compose` refuses to start with "Set X in .env" | A required variable is missing in `.env`. |
| `manifest unknown` or `denied` while pulling | `SHARPQUILL_VERSION` names a tag that does not exist (for example `latest` before the first release), or your own packages are private and the server is not logged in to the registry. |
| `umami` keeps restarting with `TypeError: Invalid URL` | `POSTGRES_PASSWORD` contains characters that are not allowed in a URL (`/`, `+`, `@`, ...). Use `openssl rand -hex 32`. The password is fixed when the database volume is first created, so on a fresh install run `docker compose down -v` and start again; on an existing install change it with `ALTER USER` first. |
| Certificates are not issued | DNS does not point at the server yet, or port 80/443 is blocked. `docker compose logs caddy`. |
| Everyone got signed out after an upgrade | `JWT_SECRET`, `DOMAIN` (token issuer) or the database changed. |
| `db-init` fails with "Authors with emails that differ only by case or spaces" | Author emails are case-insensitive since the `AuthorEmailCaseInsensitive` migration, and two existing admin accounts share one mailbox. Rename or delete one of the listed accounts (on the old version, or with `psql`), then start again. |
