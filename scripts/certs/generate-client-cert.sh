#!/bin/bash
set -euo pipefail

if [ $# -lt 1 ]; then
    echo "Usage: $0 <email> [ca-dir] [out-dir]"
    exit 1
fi

EMAIL="$1"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CA_DIR="${2:-$SCRIPT_DIR}"
OUT_DIR="${3:-$SCRIPT_DIR}"

if [ ! -f "$CA_DIR/ca.key" ] || [ ! -f "$CA_DIR/ca.crt" ]; then
    echo "ERROR: CA files not found in $CA_DIR"
    echo "Run generate-ca.sh first."
    exit 1
fi

echo "Generating client key for $EMAIL..."
openssl ecparam -genkey -name prime256v1 -noout -out "$OUT_DIR/$EMAIL.key"

echo "Generating certificate signing request..."
openssl req -new -key "$OUT_DIR/$EMAIL.key" -out "$OUT_DIR/$EMAIL.csr" \
    -subj "/CN=$EMAIL/emailAddress=$EMAIL"

echo "Signing client certificate with CA (1 year validity)..."
openssl x509 -req -in "$OUT_DIR/$EMAIL.csr" \
    -CA "$CA_DIR/ca.crt" -CAkey "$CA_DIR/ca.key" -CAcreateserial \
    -out "$OUT_DIR/$EMAIL.crt" -days 365

echo "Packaging as PKCS#12 (.p12) for browser import..."
openssl pkcs12 -export -out "$OUT_DIR/$EMAIL.p12" \
    -inkey "$OUT_DIR/$EMAIL.key" -in "$OUT_DIR/$EMAIL.crt" \
    -certfile "$CA_DIR/ca.crt" -name "$EMAIL"

# Cleanup intermediate CSR
rm -f "$OUT_DIR/$EMAIL.csr"

# Print fingerprint (SHA-256, lowercase hex, no colons — matches Caddy format)
FINGERPRINT=$(openssl x509 -in "$OUT_DIR/$EMAIL.crt" -noout -fingerprint -sha256 \
    | sed 's/.*=//; s/://g' | tr 'A-F' 'a-f')

echo ""
echo "Client certificate generated:"
echo "  Key:         $OUT_DIR/$EMAIL.key"
echo "  Cert:        $OUT_DIR/$EMAIL.crt"
echo "  PKCS#12:     $OUT_DIR/$EMAIL.p12"
echo ""
echo "SHA-256 Fingerprint (for database):"
echo "  $FINGERPRINT"
