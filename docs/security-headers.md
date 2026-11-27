# Security headers and CORS

## What the API does

- **Baseline headers** on every API response (`SecurityHeadersMiddleware`): `X-Content-Type-Options: nosniff`,
  `Referrer-Policy: strict-origin-when-cross-origin`, `X-Frame-Options: SAMEORIGIN`. Handlers that set their own
  value (for example the `/uploads` CSP for legacy SVG files) win.
- **CORS per audience** (`AudienceCorsPolicyProvider`):
  - `/api/admin/*` accepts credentialed calls from `Cors:AdminOrigins` only.
  - Everything else accepts credentialed calls from `Cors:PublicOrigins` only.
  - The admin origin may additionally read public endpoints (e.g. `/api/v1/settings`) with `GET`, without credentials.
  - If `AdminOrigins` / `PublicOrigins` are not configured, the legacy `Cors:Origins` list is used for both.

Environment variables (ASP.NET Core array syntax):

```
Cors__AdminOrigins__0=https://admin.example.com
Cors__PublicOrigins__0=https://www.example.com
```

## What the reverse proxy must do

Transport-level and page-level headers belong on the TLS-terminating proxy. A Caddy snippet to import into each site
block (adjust hosts):

```caddyfile
(security_headers) {
	header {
		# Only after you are sure every subdomain serves HTTPS; add `preload` deliberately, it is hard to undo.
		Strict-Transport-Security "max-age=31536000; includeSubDomains"
		X-Content-Type-Options "nosniff"
		Referrer-Policy "strict-origin-when-cross-origin"
		Permissions-Policy "camera=(), microphone=(), geolocation=()"
		-Server
	}
}

(admin_headers) {
	import security_headers
	header {
		X-Frame-Options "DENY"
		# Starting point for the Nuxt admin app. Nuxt SSR emits inline scripts/styles, hence 'unsafe-inline';
		# roll out with Content-Security-Policy-Report-Only first and tighten from the browser console reports.
		Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' https://www.example.com; frame-ancestors 'none'; base-uri 'self'; form-action 'self'"
	}
}

www.example.com {
	import security_headers
	# ... reverse_proxy as usual
}

admin.example.com {
	import admin_headers
	# ... mTLS / IP allowlist / reverse_proxy as usual
}
```

`connect-src` for the admin app must include the API origin it calls, and `img-src` the origin that serves uploaded media.

## Related protections (not headers)

- Requests to `/api/admin/*` should not be routed through the public hostname at all; the example deployment answers
  404 there, so credentialed CORS for the admin API is a second line of defence, not the only one.
- The proxy must overwrite or strip `X-Client-Cert-Fingerprint` on every hostname (it is only trustworthy when set
  from a verified client certificate).
