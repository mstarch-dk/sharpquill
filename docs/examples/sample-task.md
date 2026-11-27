---
id: TASK-45
title: 'Validate media uploads by content, not client-supplied type'
status: Done
assignee: []
created_date: '2026-10-17 19:42'
updated_date: '2026-10-17 19:48'
labels:
  - security
  - backend
dependencies: []
references:
  - src/backend/application/Media/Services/MediaService.cs
  - src/backend/api/Controllers/Admin/MediaController.cs
  - src/backend/application/ContentPackage/Services/ContentPackageService.cs
  - src/backend/api/Program.cs
priority: high
---

## Description

<!-- SECTION:DESCRIPTION:BEGIN -->
Media upload currently trusts the client-supplied Content-Type, keeps the client's file extension and allows SVG. Since uploads are served from the same origins as the site and admin, an uploaded HTML/SVG file could execute script (stored XSS). Uploads must be restricted to a safe set of raster image (and explicitly allowed document) types verified by file signature, stored with an extension derived from the detected type, and served with X-Content-Type-Options: nosniff.
<!-- SECTION:DESCRIPTION:END -->

## Acceptance Criteria
<!-- AC:BEGIN -->
- [x] #1 Uploads whose file content does not match an allowed type signature are rejected, regardless of declared Content-Type or extension
- [x] #2 SVG uploads are rejected
- [x] #3 Stored file extension is derived from the detected type, not the client filename
- [x] #4 Responses from /uploads include X-Content-Type-Options: nosniff
- [x] #5 Integration tests cover spoofed content type, SVG rejection, extension normalisation and the nosniff header
- [x] #6 Media imported via content packages is subject to the same signature check; entries that fail are skipped and reported as import errors
<!-- AC:END -->

## Implementation Plan

<!-- SECTION:PLAN:BEGIN -->
1. Add MediaTypeDetector (application/Media): signature check for JPEG, PNG, GIF, WebP, PDF; returns canonical MIME type + extension. No SVG.
2. MediaService.UploadAsync: read the header, detect type, reject if unknown; use the detected MIME type and extension for the stored file, variants and DB record. Remove IsAllowedMimeType and the controller's Content-Type pre-check.
3. ContentPackageService import: skip media entries whose content isn't an allowed type or whose extension doesn't match the detected type; record an import error.
4. Program.cs static files for /uploads: add X-Content-Type-Options: nosniff on all responses; add a sandboxing CSP for legacy .svg files.
5. Admin MediaPicker accept list narrowed to the allowed image types.
6. Tests: spoofed Content-Type (HTML as image/png) rejected, SVG rejected, extension derived from content (PNG uploaded as .gif/.html name stored as .png), nosniff header present, import skips non-image media.
<!-- SECTION:PLAN:END -->

## Final Summary

<!-- SECTION:FINAL_SUMMARY:BEGIN -->
Media uploads are now typed by content. Script-capable files can no longer be stored in or served from /uploads.

Changes:
- New MediaTypeDetector (application/Media/Services): recognises JPEG, PNG, GIF, WebP and PDF by file signature. SVG/HTML are never recognised.
- MediaService.UploadAsync: detects the type from the first bytes and rejects unknown content. The stored MIME type, file extension, variants and DB record all use the detected type. The client Content-Type parameter was removed from IMediaService.UploadAsync, and so were IsAllowedMimeType and the controller's Content-Type pre-check.
- Content-package import: media entries that fail detection, or whose extension doesn't match the detected type, are skipped and reported in import errors. MIME type and size come from detection and the bytes written. The unused extension-based GetMimeType helper was removed.
- /uploads static files: X-Content-Type-Options: nosniff on every response. Legacy .svg files get `Content-Security-Policy: default-src 'none'; style-src 'unsafe-inline'; sandbox` so already-uploaded SVGs can't run script.
- Admin MediaPicker accept list narrowed to jpeg/png/gif/webp.

Tests: HTML declared as PNG rejected, SVG rejected, PNG uploaded as text/html "page.html" stored as image/png with a .png name, nosniff header served, import skips HTML media. All 5 fail against the old code. Full suite 213/213.

Follow-up for the owner: existing SVG files already in production uploads are now sandboxed but still present; review and remove them if they aren't needed.
<!-- SECTION:FINAL_SUMMARY:END -->

## Definition of Done
<!-- DOD:BEGIN -->
- [x] #1 New functionality in the backend api is covered by integration tests
- [x] #2 No regressions — existing functionality still works after changes
<!-- DOD:END -->
