# Admin Security

> See also: [api-design.md](api-design.md) for API authentication details, [backend-code-structure.md](backend-code-structure.md) for controller patterns

## Overview

The admin panel (served on its own hostname, `admin.example.com` in the examples) supports two authentication methods:

1. **Password + JWT** — Traditional email/password login producing JWT access and refresh tokens
2. **Client Certificate** — TLS mutual authentication via client certificates, validated by Caddy

Both methods produce the same JWT tokens for subsequent API calls. Certificate auth replaces the password step, not the JWT session.

## Certificate Authentication Flow

```
Browser                    Caddy                     API (port 5000)
  │                          │                            │
  ├──TLS handshake──────────►│                            │
  │  (presents client cert)  │                            │
  │                          │──validates cert against──► │
  │                          │  CA (deploy/certs/ca.crt)  │
  │                          │                            │
  │◄──TLS established────────│                            │
  │                          │                            │
  ├──POST /api/admin/auth/cert-login──────────────────────►
  │                          │  adds header:              │
  │                          │  X-Client-Cert-Fingerprint │
  │                          │                            │
  │                          │              ┌─────────────┤
  │                          │              │ Lookup by    │
  │                          │              │ fingerprint  │
  │                          │              │ in authors   │
  │                          │              │ table        │
  │                          │              └─────────────┤
  │                          │                            │
  │◄──────────────────────JWT tokens──────────────────────│
```

### Step-by-step

1. **TLS handshake** — The browser presents its client certificate during the TLS handshake with Caddy. The `CertificateVerify` message proves the browser holds the private key.
2. **CA validation** — Caddy validates the certificate chain against the trusted CA (`deploy/certs/ca.crt`, mounted into the Caddy container as `/etc/caddy/certs/ca.crt`). Untrusted or expired certs are rejected. Caddy's TLS mode is `request` (not `require`), so connections without certs are allowed — they simply won't have a fingerprint header.
3. **Header forwarding** — Caddy extracts the SHA-256 fingerprint and passes it to the backend via `X-Client-Cert-Fingerprint` header.
4. **Backend lookup** — `AuthController.CertLogin()` reads the header and calls `AuthService.CertLoginAsync()`, which looks up the author by `certificate_thumbprint` column.
5. **JWT issuance** — On match, standard JWT access + refresh tokens are issued, identical to password login.

## Why Thumbprint-Only Storage Is Sufficient

The database stores only the SHA-256 fingerprint of the certificate — not the public key or full certificate. This is secure because:

- **The fingerprint is an identifier, not a secret.** It maps a validated certificate to an author. The actual cryptographic proof happens during the TLS handshake (step 1 above), which Caddy handles before the request reaches the backend.
- **The CA chain prevents forgery.** An attacker cannot create a certificate with a matching fingerprint without the CA private key, because Caddy rejects certificates not signed by the trusted CA.
- **No replay risk.** The fingerprint alone cannot authenticate — the browser must prove private key possession during TLS. Knowing someone's fingerprint doesn't help without their private key.

Storing the full public key would add no security benefit — Caddy already validates the certificate chain. The fingerprint is simply a stable lookup key.

## Moving Parts Inventory

| Component | Location | Purpose |
|-----------|----------|---------|
| CA certificate | `deploy/certs/ca.crt` (on the server, mounted at `/etc/caddy/certs/ca.crt`) | Caddy uses this to validate client certs |
| CA private key | Offline / secure storage | Signs new client certs (never on API server) |
| Caddy TLS config | `deploy/Caddyfile.example` — admin host block (commented `tls { client_auth ... }`) | `client_auth` with `mode request` + CA trust |
| Header forwarding | `deploy/Caddyfile.example` — `header_up X-Client-Cert-Fingerprint` (stripped on every other route) | Passes fingerprint from Caddy to API |
| DB column | `authors.certificate_thumbprint` | Stores SHA-256 fingerprint per author |
| Unique index | On `certificate_thumbprint` (filtered, non-null) | Ensures one cert per author |
| Auth controller | `src/backend/api/Controllers/Admin/AuthController.cs` | `cert-login` and `register-cert` endpoints |
| Auth service | `src/backend/application/Auth/Services/AuthService.cs` | `CertLoginAsync()` — lookup + JWT issuance |
| Author repository | `src/backend/application/Authors/Repositories/AuthorRepository.cs` | `GetByCertificateThumbprintAsync()` |
| CA generation script | `scripts/certs/generate-ca.sh` | Creates EC P-256 CA key + self-signed cert |
| Client cert script | `scripts/certs/generate-client-cert.sh` | Creates client cert, signs with CA, outputs .p12 |

## Certificate Lifecycle

### 1. Generate CA (one-time)

```bash
cd scripts/certs
./generate-ca.sh
```

Creates `ca.key` (EC P-256) and `ca.crt` (10-year validity). Copy `ca.crt` to `deploy/certs/ca.crt` on the server and uncomment the `client_auth` block in your Caddyfile. Store `ca.key` securely offline.

### 2. Generate client certificate

```bash
./generate-client-cert.sh user@example.com
```

Outputs:
- `user@example.com.key` — private key
- `user@example.com.crt` — signed certificate (1-year validity)
- `user@example.com.p12` — PKCS#12 bundle for browser import
- SHA-256 fingerprint (lowercase hex, no colons — matches Caddy format)

### 3. Register thumbprint

Two options:

**Option A** — Via admin API (author must be logged in with password first):
```
POST /api/admin/auth/register-cert
```
With the client cert installed in the browser, this endpoint reads the fingerprint from the header and stores it against the logged-in author.

**Option B** — Manual database update:
```sql
UPDATE authors SET certificate_thumbprint = '<fingerprint>' WHERE email = 'user@example.com';
```

### 4. Install in browser

Import the `.p12` file into the browser's certificate store. The browser will prompt to select it when connecting to the admin host.

### 5. Renewal

Client certificates expire after 1 year. To renew:

1. Generate a new client certificate with the same script
2. Install the new `.p12` in the browser
3. Update the fingerprint via `register-cert` endpoint or manual DB update
4. Remove the old certificate from the browser

### 6. CA rotation

If the CA needs rotation (compromise or expiry):

1. Generate a new CA with `generate-ca.sh` (after removing old `ca.key`)
2. Deploy new `ca.crt` to `deploy/certs/ca.crt` on the server
3. Reload Caddy (`caddy reload`)
4. Re-issue all client certificates signed by the new CA
5. All authors must install new `.p12` files and re-register fingerprints

## Security Considerations

### Port isolation

The API (port 5000) must not be directly accessible from outside the server. The backend trusts the `X-Client-Cert-Fingerprint` header unconditionally — if an attacker could reach port 5000 directly, they could set this header to any value and bypass certificate auth.

**Current mitigation**: The API binds to `127.0.0.1:5000` within Docker and is not exposed externally. Only Caddy (the reverse proxy) can reach it. Verify this remains true when changing Docker or firewall configuration.

### Header trust model

The `X-Client-Cert-Fingerprint` header is a trusted internal header set by Caddy. The backend does not independently validate the certificate. This is secure as long as:

- Port 5000 is not externally reachable
- No other reverse proxy or service can inject this header
- Caddy is the sole entry point for the admin host, and it strips any client-supplied `X-Client-Cert-Fingerprint` on every route

### Certificate and key storage

- **CA private key** — Must never be on the API server. Keep offline or in a hardware security module. Anyone with this key can mint valid client certificates.
- **Client `.p12` files** — Contain the private key. Delete from the server after the author has downloaded them. Never transmit over unencrypted channels.
- **`scripts/certs/.gitignore`** — Already configured to exclude `*.key`, `*.crt`, `*.p12`, `*.csr`, `*.srl` from version control.

### Why not a "Create Certificate" button in the admin panel?

Generating client certificates requires the CA private key. Placing the CA key on the API server would mean:

- A server compromise exposes the CA key, allowing unlimited certificate forgery
- The CA key would need to be accessible to the application process

The current approach — generating certificates offline using the shell scripts — keeps the CA key isolated from the running system. This is the recommended practice.
