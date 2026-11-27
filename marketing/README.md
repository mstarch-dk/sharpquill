# Marketing material

An English brochure for the CMS: [sharpquill-brochure.pdf](sharpquill-brochure.pdf) (A4, the committed edition), built from
[site/index.html](site/index.html) (the page; light and dark, works on a phone). It is written for two readers at
once: people who write (what the CMS lets them do) and people who run servers (section 09, *Running it yourself*).

**Every screenshot comes from a demo installation with fictional content** ([demo/seed.yaml](demo/seed.yaml)): a
Danish gardening blog, *Nyttehaven*, with the site language set to Danish. The demo runs
as its own Docker Compose project, `cms-demo`, with the settings in [demo/demo.env](demo/demo.env): its own ports
(web 4000, admin 4001, API 5100), database and uploads volumes, so it never touches the dev stack or its data.

The screenshots (`site/img/`) and the generated cover images (`demo/images/`) are build output and not committed.
The two README screenshots in `docs/images/` are copies of the `web_article` and `admin_editor` shots.

## Updating

Run `/marketing` in Claude Code. It finds what changed in the app since the last edition
([edition.json](edition.json)), updates the seed, shots and text, rebuilds and checks.

By hand, from this folder (`npm ci` and `npx playwright install --with-deps chromium` once):

| Command | Does |
|---|---|
| `npm run marketing -- demo-up` | build and start the demo stack (`docker compose -p cms-demo …`), wait until healthy |
| `npm run marketing -- seed` | fill the empty demo database from `demo/seed.yaml` through the CMS's own API |
| `npm run marketing -- explore web:/path admin:/path …` | full-page captures in `.explore/`, for choosing frames |
| `npm run marketing -- shots [id…]` | [shots.yaml](shots.yaml) → `site/img/` (and the `also:` copies) |
| `npm run marketing -- pdf` | `site/index.html` → `sharpquill-brochure.pdf` |
| `npm run marketing -- check` | features ↔ page ↔ shots, images present, demo hosts and emails only, demo database, phone width |
| `npm run marketing -- edition [--date D]` | write `edition.json` |
| `npm run marketing -- demo-down` | stop the demo and delete its volumes |
| `npm run marketing -- all` | demo-down, demo-up, images, seed, shots, pdf, check |

`seed` needs an empty demo database; to start over run `demo-down` and `demo-up`. The public API rate-limits
comments and likes per address, so a seed takes about three minutes.

## Files

| File | What |
|---|---|
| [marketing.mjs](marketing.mjs) | the tool (Node + Playwright) |
| [demo/demo.env](demo/demo.env) | demo stack configuration (throwaway values only) |
| [demo/seed.yaml](demo/seed.yaml) | the fictional site: settings, writers, articles, series, menus, readers, comments, likes |
| [shots.yaml](shots.yaml) | every screenshot: page, element to crop to, actions, viewport, colour scheme |
| [features.yaml](features.yaml) | the feature inventory: brochure section, routes, controllers, shots |
| [site/index.html](site/index.html) | the brochure |
| [edition.json](edition.json) | what the last edition covered (commit, date, features, shots, pages) |
