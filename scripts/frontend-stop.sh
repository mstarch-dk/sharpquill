#!/bin/bash

# Stop and remove the frontend containers (web, admin) without affecting backend services.

set -e

cd "$(dirname "$0")/.."

docker compose stop web admin
docker compose rm -f web admin

echo ""
echo "Frontend containers stopped. Backend services still running:"
docker compose ps
