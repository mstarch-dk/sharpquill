#!/bin/bash

# Build and start frontend containers for integration testing.
# Rebuilds admin and web images from scratch, then starts them alongside
# the already-running backend services.

set -e

cd "$(dirname "$0")/.."

echo "Building frontend containers (no cache)..."
docker compose build --no-cache admin web

echo "Starting frontend containers..."
docker compose up -d admin web

echo ""
echo "Waiting for containers to be ready..."
sleep 3

echo ""
docker compose ps

echo ""
echo "Admin:  http://localhost:3001"
echo "Web:    http://localhost:3000"
echo ""
echo "Run 'scripts/frontend-stop.sh' when done testing."
