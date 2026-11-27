---
name: marketing
description: Build or refresh the English marketing brochure for the CMS (marketing/site/index.html and marketing/sharpquill-brochure.pdf) with fresh screenshots from a demo installation that holds only fictional content. Detects features added since the last edition and adds sections and shots for them; also refreshes the README screenshots in docs/images. Use when the user asks for marketing material, a brochure, a feature overview or screenshots of the CMS, asks to update them after new features, or runs /marketing.
---

# Marketing material

Produce an up-to-date English brochure (HTML page and A4 PDF) that shows two readers what the CMS does: people who
write (what they can do with it) and people who run servers (how it is deployed and secured). Every screenshot comes
from the real app running in a demo installation filled with a **fictional** site. Every run is a new *edition*: it
starts from the last one (`marketing/edition.json`) and brings it up to date with the app as it is now.

Everything lives in `marketing/` (see its README). All of it is committed except build output: the screenshots
(`site/img/`), the generated cover images (`demo/images/`), `.explore/` and `node_modules/`.
- `demo/demo.env`: the demo stack's config (compose project `cms-demo`, ports web 4000 / admin 4001 / API 5100)
- `demo/seed.yaml`: the fictional site (settings, writers, articles, series, menus, readers, comments, likes)
- `shots.yaml`: every screenshot (app, path, element to crop to, actions, viewport, colour scheme, `also:` copies)
- `features.yaml`: the feature inventory: brochure section, web/admin routes, API controllers, shots
- `site/index.html`: the brochure; `sharpquill-brochure.pdf`: the edition; `edition.json`: what the last edition covered

The tool is `marketing/marketing.mjs`, run from `marketing/` as `npm run marketing -- <command>` (`npm ci` and
`npx playwright install --with-deps chromium` the first time). Commands: `demo-up`, `demo-down`, `images`, `seed`,
`explore web:/path admin:/path …`, `shots [id…]`, `pdf`, `check`, `edition [--date D]`, `all`.

## Ground rules
- **Never show real data.** The demo is its own compose project with its own volumes; never point the tool at the
  dev stack (ports 3000/3001/5000) or at a production site, never copy a real database or uploads into the demo, and
  never put a real person, address, site or customer into `seed.yaml` (emails on `.example` domains only).
  `check` refuses hosts and emails outside the allowed list and confirms the demo database holds only the seed's
  writers and articles.
- The seed goes through the CMS's own API, so it exercises the same validation as the admin panel. If the API
  rejects seed data, fix the seed (or report the app bug), don't work around the API.
- **Facts come from the code and docs.** Every number and claim in the text (theme and font counts, image limits and
  variant widths, token lifetimes, block types, roles) is re-checked for this edition in the code
  (`src/backend/application`, `src/frontend/*/app`, `src/frontend/themes`) or `docs/`. Never carry a claim over from
  the last edition unchecked, and don't describe behaviour you haven't confirmed (for example whether a choice is
  remembered, or how deep replies nest).
- The demo content is **Danish** and its site language is `da`, so the public site's own texts are Danish too
  (they ship in Danish and English, `src/frontend/web/i18n/locales/`). Web locators that match those texts
  (`has_text`, aria labels) use the Danish strings from `da.json`. The brochure is **English** and says so.

## Steps

1. **What changed since the last edition.**
   - Read `marketing/edition.json`, then `git log --oneline <commit>..HEAD` and
     `git diff --stat <commit> -- src/frontend/web/app/pages src/frontend/admin/app/pages src/frontend/admin/app/components src/backend/api/Controllers src/frontend/themes docs`.
     Also look at uncommitted work (`git status`): the demo builds from the working tree.
   - Compare with `features.yaml`: list **new** features (a page, a block type, a setting, a controller), **changed**
     ones (new fields, options, limits) and **removed** ones. Read the code and docs for anything unclear.
   - Don't ask the user to confirm: add, update or remove features, shots and sections on your own, and report what
     you did at the end.
2. **The demo world.**
   - `demo-down`, then `demo-up` (builds the images from the working tree and waits until healthy), then `seed`.
     The public API rate-limits comments and likes, so the seed waits about two minutes; that is expected.
   - If a new feature needs content to show anything, add it to `seed.yaml` (and to `seed()` in `marketing.mjs` if
     the API needs a new call). Restart from `demo-down`, so the seed stays the only source.
   - Quote YAML strings that contain commas inside `{…}` flow mappings, or the text gets cut at the comma.
3. **Explore** new or changed pages.
   - `explore web:/path admin:/path …` saves full-page captures to `marketing/.explore/`; look at them with Read.
   - Find locators by dumping headings, buttons and labels with a short Playwright script, or by reading the Vue
     component. Prefer headings, labels and `:has(h2:text-is('…'))` over classes; note that `has_text` regexes
     match the element's text including leading whitespace, so anchor with `^\\s*…`.
   - Pick frames that show the feature with data in it: no empty states, error banners or spinners.
   - Add or adjust entries in `shots.yaml` (crop to elements, `to:` for a range, `span:` for the width, `{article:SLUG}`
     style placeholders for ids), then link each shot to its feature in `features.yaml`.
4. **Capture**: `shots` (or `shots ID…` while iterating). Then **look at every new or changed image** with Read
   (a contact sheet of several images in one picture saves time):
   - data loaded, images visible (the dev web container proxies `/uploads` to the API, as Caddy does in production)
   - the crop shows what the caption will claim, with nothing half cut at the edges
   - only demo names and `.example` addresses appear
5. **Write and update the text** in `marketing/site/index.html`.
   - Keep the structure: hero, *What it is, and who it is for*, the numbered feature sections, *Running it
     yourself*, *Strengths and limits*, *Get started*, footer.
   - A new feature gets its own section when it changes how someone works, otherwise a paragraph and a figure in an
     existing section. Update *Strengths and limits* when a limit goes away or a new one appears, and the footer's
     screenshot date.
   - Every `section` in `features.yaml` must exist as an `id` in the page, and every shot must be used exactly once.
6. **Build and check.**
   - `pdf`, then `check`, and fix everything `check` reports.
   - Render the PDF pages and look at them (for example PyMuPDF in a scratch venv, six pages per contact sheet).
     Fix a figure split from its caption, a nearly empty page or an image too small to read with the
     `@media print` rules and the `narrow` figure class in index.html.
   - Then `edition --date <today>`.
7. **Report.**
   - Send the PDF with SendUserFile and list what changed in this edition: new or updated sections, new and removed
     shots, seed changes, and whether `docs/images/` changed.
   - List anything in the app that looked wrong in the screenshots. That is an app bug to raise, not to hide with a
     crop.
   - `demo-down` when done. Don't commit unless asked.

## Writing
- **Audience:** writers and small publishers who want their own site, and developers or operators who would run it.
  Lead each section with what it lets you do, then how it works, then where it stops.
- **Plain and concrete, no hype.** Not: revolutionary, seamless, effortless, powerful, best-in-class, superlatives,
  exclamation marks. Prefer a concrete example from the screenshot to an adjective.
- **App labels** in italics as they appear in the admin (*Simple Editor*, *Versions*, *Packages*); Danish texts from
  the public site get an English gloss the first time (*Del 4 af 4*, “part 4 of 4”).
- **Captions:** a bold label, then one or two sentences on what the picture shows and how to read it.
