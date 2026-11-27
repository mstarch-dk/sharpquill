# Sharpquill brand assets

The Sharpquill mark is a minimalist, sharp quill. The brand is written **Sharpquill** (one word, lower-case q) in
everything people read; code identifiers keep `SharpQuill` (for example the `SharpQuill.*` .NET namespaces).

The shapes come from a first identity direction generated with ChatGPT and were recoloured to the Ink teal palette.
All files are plain SVG with vector paths; the wordmark is live text in a system sans-serif stack (Inter, then
ui-sans-serif / system-ui), so it can render slightly differently from machine to machine.

## Palette (Ink teal)

| Role             | Hex       |
| ---------------- | --------- |
| Primary          | `#0F3D3E` |
| Accent           | `#1F7A72` |
| Light accent     | `#7CC4B8` |
| Dark background  | `#0A2324` |
| Light background | `#FFFFFF` |

The admin panel's Tailwind `primary` scale (`src/frontend/admin/tailwind.config.ts`) is built around these:
`600` is the accent, `900` the primary, `950` the dark background.

## Files

| File | Use |
| ---- | --- |
| `logo.svg` | Horizontal logo (mark and wordmark) for light surfaces |
| `logo-dark.svg` | White logo for dark surfaces |
| `icon.svg` | Standalone mark, primary colour |
| `icon-dark.svg` | White mark for dark surfaces |
| `icon-accent.svg` | Mark in the accent colour |
| `icon-monochrome.svg` | Near-black mark for single-colour use |
| `icon-adaptive.svg` | Follows the viewer's light/dark mode (CSS system colours `CanvasText` / `Canvas`) |
| `favicon.svg`, `app-icon-dark.svg` | Square app icon: white mark on the primary colour |
| `app-icon-light.svg` | Square app icon: primary mark on white |
| `favicon-16.png`, `favicon-32.png`, `favicon-48.png` | Browser tab fallbacks |
| `apple-touch-icon.png` | 180 px iOS home-screen icon (full-bleed; iOS rounds the corners itself) |
| `favicon-192.png`, `favicon-512.png` | Web app manifest icons |

The standalone marks can be recoloured: they paint with `currentColor`, and the thin cutout uses the
`--sq-cutout` custom property (set it to the surface colour behind the mark).

## Where they are used

- Admin panel: the favicon set and `site.webmanifest` in `src/frontend/admin/public/`, the logo in the sidebar and on
  the login page (light and dark variants), the mark in the collapsed sidebar.
- Public website: `favicon.svg`, `favicon-32.png` and `apple-touch-icon.png` as the default favicon only. The site
  pages themselves carry the site owner's brand, not Sharpquill's.
- README: `logo.svg` / `logo-dark.svg` follow GitHub's light and dark theme.

## Regenerating the PNGs

The PNGs are rendered from `favicon.svg` at exact pixel sizes with Chromium (Playwright, as used by `marketing/`).
After changing `favicon.svg`, re-render all six and copy them to the admin and web `public/` folders.
