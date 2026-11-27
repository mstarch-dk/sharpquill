#!/usr/bin/env bash
#
# Back up a CMS stack started from deploy/compose.yml: the cms database, the umami database,
# cluster globals (roles) and the uploads volume.
#
# Usage:   scripts/backup.sh
# Env:     DEPLOY_DIR       directory with compose.yml and .env   (default: <repo>/deploy)
#          BACKUP_DIR       where backups are written             (default: <DEPLOY_DIR>/backups)
#          RETENTION_DAYS   delete backups older than this        (default: 14)
#
# Each run creates BACKUP_DIR/<timestamp>/ containing cms.dump, umami.dump, globals.sql.gz,
# uploads.tar.gz, rowcounts-*.tsv, SHA256SUMS and manifest.txt. The directory only appears under its
# final name once every artifact has been written AND verified; any failing step aborts the run with a
# non-zero exit status and leaves no half-finished backup behind that looks complete.
#
# Cron example (daily 03:00):  0 3 * * * /srv/cms/scripts/backup.sh >> /var/log/cms-backup.log 2>&1

# shellcheck disable=SC2016  # single quotes are intentional: $POSTGRES_* must expand inside the db container
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="${DEPLOY_DIR:-${SCRIPT_DIR}/../deploy}"
DEPLOY_DIR="$(cd "${DEPLOY_DIR}" && pwd)"
BACKUP_DIR="${BACKUP_DIR:-${DEPLOY_DIR}/backups}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"
UMAMI_DB="umami"

dc() { docker compose --project-directory "${DEPLOY_DIR}" -f "${DEPLOY_DIR}/compose.yml" "$@"; }
log() { echo "[$(date '+%F %T')] $*"; }
die() { echo "ERROR: $*" >&2; exit 1; }

[ -f "${DEPLOY_DIR}/compose.yml" ] || die "compose.yml not found in ${DEPLOY_DIR}"

# Backups contain password hashes and all content: readable by the owner only
umask 077

TIMESTAMP="$(date +%Y%m%d_%H%M%S)"
FINAL_DIR="${BACKUP_DIR}/${TIMESTAMP}"
WORK_DIR="${BACKUP_DIR}/.partial-${TIMESTAMP}"
mkdir -p "${WORK_DIR}"
on_exit() {
    local status=$?
    if [ "${status}" -ne 0 ]; then
        echo "ERROR: backup failed (exit ${status}); partial data kept in ${WORK_DIR}" >&2
    fi
}
trap on_exit EXIT

# Run a command inside the db container, using its own POSTGRES_USER / POSTGRES_DB
db_sh() { dc exec -T db sh -c "$1"; }

# "table<TAB>exact row count" for every table of a database, sorted
rowcounts() {
    db_sh "psql -U \"\$POSTGRES_USER\" -d '$1' -At -F '	' -c \"
        SELECT table_name,
               (xpath('/row/c/text()', query_to_xml(format('SELECT count(*) AS c FROM %I.%I', table_schema, table_name), false, true, '')))[1]::text
        FROM information_schema.tables
        WHERE table_schema = 'public' AND table_type = 'BASE TABLE'
        ORDER BY 1\""
}

log "Checking the database is reachable"
db_sh 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"' >/dev/null || die "database container is not running or not ready"
CMS_DB="$(db_sh 'printf %s "$POSTGRES_DB"')"

log "Dumping ${CMS_DB} database"
db_sh 'pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc' > "${WORK_DIR}/cms.dump"

log "Dumping ${UMAMI_DB} database"
db_sh "pg_dump -U \"\$POSTGRES_USER\" -d '${UMAMI_DB}' -Fc" > "${WORK_DIR}/umami.dump"

log "Dumping globals (roles)"
db_sh 'pg_dumpall -U "$POSTGRES_USER" --globals-only' | gzip > "${WORK_DIR}/globals.sql.gz"

log "Archiving uploads"
dc exec -T api tar -czf - -C /app/uploads . > "${WORK_DIR}/uploads.tar.gz"

log "Recording row counts"
rowcounts "${CMS_DB}" > "${WORK_DIR}/rowcounts-cms.tsv"
rowcounts "${UMAMI_DB}" > "${WORK_DIR}/rowcounts-umami.tsv"
[ -s "${WORK_DIR}/rowcounts-cms.tsv" ] || die "no tables found in ${CMS_DB}"

log "Verifying artifacts"
db_sh 'pg_restore --list >/dev/null' < "${WORK_DIR}/cms.dump" || die "cms.dump is not a valid dump"
db_sh 'pg_restore --list >/dev/null' < "${WORK_DIR}/umami.dump" || die "umami.dump is not a valid dump"
gzip -t "${WORK_DIR}/globals.sql.gz" || die "globals.sql.gz is corrupt"
tar -tzf "${WORK_DIR}/uploads.tar.gz" >/dev/null || die "uploads.tar.gz is corrupt"

(
    cd "${WORK_DIR}"
    sha256sum cms.dump umami.dump globals.sql.gz uploads.tar.gz rowcounts-cms.tsv rowcounts-umami.tsv > SHA256SUMS
    sha256sum --check --quiet SHA256SUMS
)

{
    echo "created=${TIMESTAMP}"
    echo "cms_database=${CMS_DB}"
    echo "umami_database=${UMAMI_DB}"
    echo "postgres_version=$(db_sh 'postgres --version')"
    echo "cms_tables=$(wc -l < "${WORK_DIR}/rowcounts-cms.tsv")"
} > "${WORK_DIR}/manifest.txt"

mv "${WORK_DIR}" "${FINAL_DIR}"
trap - EXIT
log "Backup complete: ${FINAL_DIR} ($(du -sh "${FINAL_DIR}" | cut -f1))"

log "Removing backups older than ${RETENTION_DAYS} days"
find "${BACKUP_DIR}" -mindepth 1 -maxdepth 1 -type d -name '20*' -mtime "+${RETENTION_DAYS}" -exec rm -rf {} +
