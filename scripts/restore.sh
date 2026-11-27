#!/usr/bin/env bash
#
# Restore a backup made by scripts/backup.sh into a stack started from deploy/compose.yml.
# The existing cms and umami databases and the uploads volume are REPLACED.
#
# Usage:   scripts/restore.sh <backup-directory> [--yes]
# Env:     DEPLOY_DIR   directory with compose.yml and .env   (default: <repo>/deploy)
#
# Typical use on a fresh server: configure deploy/.env, start the database (`docker compose up -d db`),
# create the api container (`docker compose create api`), then run this script. See docs/deployment.md.

# shellcheck disable=SC2016  # single quotes are intentional: $POSTGRES_* must expand inside the db container
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="${DEPLOY_DIR:-${SCRIPT_DIR}/../deploy}"
DEPLOY_DIR="$(cd "${DEPLOY_DIR}" && pwd)"
UMAMI_DB="umami"

dc() { docker compose --project-directory "${DEPLOY_DIR}" -f "${DEPLOY_DIR}/compose.yml" "$@"; }
log() { echo "[$(date '+%F %T')] $*"; }
die() { echo "ERROR: $*" >&2; exit 1; }

BACKUP="${1:-}"
[ -n "${BACKUP}" ] || die "Usage: $0 <backup-directory> [--yes]"
[ -d "${BACKUP}" ] || die "backup directory not found: ${BACKUP}"
BACKUP="$(cd "${BACKUP}" && pwd)"
ASSUME_YES="${2:-}"

for f in cms.dump umami.dump globals.sql.gz uploads.tar.gz rowcounts-cms.tsv rowcounts-umami.tsv SHA256SUMS; do
    [ -f "${BACKUP}/${f}" ] || die "missing ${f} in ${BACKUP}"
done

db_sh() { dc exec -T db sh -c "$1"; }

rowcounts() {
    db_sh "psql -U \"\$POSTGRES_USER\" -d '$1' -At -F '	' -c \"
        SELECT table_name,
               (xpath('/row/c/text()', query_to_xml(format('SELECT count(*) AS c FROM %I.%I', table_schema, table_name), false, true, '')))[1]::text
        FROM information_schema.tables
        WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
        ORDER BY 1\""
}

log "Verifying backup checksums"
(cd "${BACKUP}" && sha256sum --check --quiet SHA256SUMS) || die "checksum mismatch, refusing to restore"

# A database that was just started needs a few seconds before it accepts connections
for _ in $(seq 1 30); do
    db_sh 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"' >/dev/null 2>&1 && break
    sleep 2
done
db_sh 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"' >/dev/null || die "database container is not running or not ready after 60 s (docker compose up -d db)"
CMS_DB="$(db_sh 'printf %s "$POSTGRES_DB"')"

if [ "${ASSUME_YES}" != "--yes" ]; then
    echo "This will REPLACE the '${CMS_DB}' and '${UMAMI_DB}' databases and the uploads of the stack in ${DEPLOY_DIR}"
    read -r -p "Type 'restore' to continue: " answer
    [ "${answer}" = "restore" ] || die "aborted"
fi

log "Stopping application containers"
dc stop api web admin umami caddy >/dev/null

log "Restoring globals (roles that already exist are skipped)"
globals_log="$(mktemp)"
trap 'rm -f "${globals_log}"' EXIT
gzip -dc "${BACKUP}/globals.sql.gz" | db_sh 'psql -U "$POSTGRES_USER" -d postgres -q' >/dev/null 2>"${globals_log}" || true
if grep -i "error" "${globals_log}" | grep -vi "already exists" | grep -q .; then
    cat "${globals_log}" >&2
    die "restoring globals failed"
fi

restore_db() {
    local name="$1" dump="$2"
    log "Restoring database ${name}"
    db_sh "psql -U \"\$POSTGRES_USER\" -d postgres -v ON_ERROR_STOP=1 -q \
        -c \"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '${name}' AND pid <> pg_backend_pid()\" \
        -c \"DROP DATABASE IF EXISTS \\\"${name}\\\"\" \
        -c \"CREATE DATABASE \\\"${name}\\\" OWNER \\\"\$POSTGRES_USER\\\"\"" >/dev/null
    db_sh "pg_restore -U \"\$POSTGRES_USER\" -d '${name}' --no-owner --exit-on-error" < "${dump}"
}
restore_db "${CMS_DB}" "${BACKUP}/cms.dump"
restore_db "${UMAMI_DB}" "${BACKUP}/umami.dump"

log "Restoring uploads"
dc run -T --rm --no-deps -u 0 --entrypoint sh api -c 'find /app/uploads -mindepth 1 -delete && tar -xzpf - -C /app/uploads && chown -R 1001:1001 /app/uploads' < "${BACKUP}/uploads.tar.gz"

log "Comparing row counts with the backup"
status=0
for pair in "${CMS_DB}:cms" "${UMAMI_DB}:umami"; do
    db="${pair%%:*}"; label="${pair##*:}"
    rowcounts "${db}" > "${BACKUP}/.restored-rowcounts-${label}.tsv"
    if diff -u "${BACKUP}/rowcounts-${label}.tsv" "${BACKUP}/.restored-rowcounts-${label}.tsv"; then
        log "${db}: row counts match ($(wc -l < "${BACKUP}/rowcounts-${label}.tsv") tables)"
    else
        echo "ERROR: row counts differ for ${db}" >&2
        status=1
    fi
    rm -f "${BACKUP}/.restored-rowcounts-${label}.tsv"
done
[ "${status}" -eq 0 ] || die "restore completed but verification failed"

log "Starting the stack"
dc up -d >/dev/null
log "Restore complete"
