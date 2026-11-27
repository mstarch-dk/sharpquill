#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
OUT_DIR="${1:-$SCRIPT_DIR}"

if [ -f "$OUT_DIR/ca.key" ]; then
    echo "ERROR: CA key already exists at $OUT_DIR/ca.key"
    echo "Remove existing CA files first if you want to regenerate."
    exit 1
fi

echo "Generating CA private key (EC P-256)..."
openssl ecparam -genkey -name prime256v1 -noout -out "$OUT_DIR/ca.key"

echo "Generating self-signed CA certificate (10 year validity)..."
openssl req -new -x509 -key "$OUT_DIR/ca.key" -out "$OUT_DIR/ca.crt" \
    -days 3650 \
    -subj "/CN=CMS Admin CA/O=CMS"

echo ""
echo "CA files generated:"
echo "  Key:  $OUT_DIR/ca.key"
echo "  Cert: $OUT_DIR/ca.crt"
