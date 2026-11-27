## Frontend UIs

> See also: [api-design.md](api-design.md) for API response/error formats consumed by frontends, [project-structure.md](project-structure.md) for directory layout under `src/frontend/`

### Frontends

There are TWO frontends:

- 'admin' is the UI used for editing and creating content.
- 'web' is the public-facing website UI that displays content to readers of the CMS/Blog.
- Both are Nuxt 4 + TypeScript websites that use the backend api to access and manipulate content
- Code both need lives once, in the shared Nuxt layer `src/frontend/layers/shared` (below); the themes are shared
  through `src/frontend/themes`.

### Shared layer (`src/frontend/layers/shared`)

Both `nuxt.config.ts` files have `extends: ['../layers/shared']`. The layer's `app/components`, `app/composables` and
`app/utils` auto-import in both apps as if they were the app's own; its types (and utils a file imports explicitly)
are imported as `#layer/...` (e.g. `import type { RenderedLayoutDto } from '#layer/types/layout'`). It holds:

- **utils**: `sanitizeHtml` / `safeHref` (every `v-html` and author-controlled `href`), `suggestionHighlight`,
  `featureIcons`, `featuredImage` (size helpers; the admin keeps its editor-only variant hints in
  `utils/featuredImageVariants.ts`).
- **components**: `ArticleLayout` and the block renderers (below), `LinkCard`, `TweetCard`, `FeatureIcon`,
  `UiLoadingSpinner`, and the `UiIcon` renderer. The icon *sets* stay per app (`~/utils/icons`; the website uses
  2px-stroke icons, the admin Heroicons outline), so `UiIcon` imports `~/utils/icons`, which resolves to the consuming app.
- **types**: the API envelope (`types/api.ts`) and the layout DTOs (`types/layout.ts`). The apps' own `types/*.ts`
  re-export them, so existing `~/types/...` imports keep working.
- **app config**: `sharedLayer.darkModeSwapsPrimary`, true in the website (its themes swap the primary scale in dark
  mode) and false in the admin, so shared components know whether to add `dark:` primary variants.

Rules that keep one layer working in two apps:

- **No npm dependencies of its own.** The layer has no `node_modules` (its `package.json` only marks the config files
  as ES modules). A package layer code imports must be a dependency of *both* apps and be listed in `peerDependencies`
  in `layers/shared/paths.ts`: Vite resolves it from the consuming app (`resolve.dedupe`) and TypeScript gets a path to
  the app's copy. Do not alias it to an absolute path instead: the server build would then `require` files from the
  build machine's `node_modules`, which the runtime image does not have. Packages imported only for types (`vue`, and
  in tests `vitest` / `@nuxt/test-utils/runtime`) are in `typeOnlyDependencies`.
- **UI text** goes through `useLayerText().t(key)`: the website's i18n translates the key (add it to every
  `web/i18n/locales` file); the admin, which has no i18n yet, shows the English fallback in `useLayerText.ts`.
- **App-only code stays out of it**, with one exception: the block editors (below), which only the admin loads.
- **Docker and CI**: both Dockerfiles copy `layers/` next to the app (build context `src/frontend`), and CI runs the
  web and admin jobs when `src/frontend/layers/**` changes. `npm run lint`, `npm run typecheck` and `npm test` in either
  app cover the layer too.

### Layouts and blocks

An article or page either has Markdown content or a *layout*: sections (full width, two or three columns, with an
optional background style) holding blocks (text, image, cards, steps, CTA, link, code, tweet, component, container).
The admin edits the stored form (`BlockInputDto`: Markdown and media ids); the API renders it (`RenderedBlockDto`:
sanitised HTML and media URLs) for the website and the admin preview, and validates every layout it stores.

Each block type is one folder in the shared layer, found by file name, so the website, the admin editor and the admin
preview pick a new block up without any list to edit:

```
layers/shared/app/blocks/<type>/
  index.ts     definition: type (= folder name), picker label and order, the empty block the editor inserts
  Render.vue   renders the RenderedBlockDto (props `block`, `preview`) - website and admin preview
  Editor.vue   edits the BlockInputDto (props `block`, `depth`, emits `update`) - admin only
layers/shared/app/embeds/<Name>/
  index.ts     embed for the "component" block: name (= folder name), label, lazy component import
  <Name>.vue   the component (rendered on the website only)
```

- `blocks/registry.ts` and `embeds/registry.ts` collect the folders with `import.meta.glob`; `blocks/editors.ts`
  collects the editors and is imported only by the admin's `BlockEditor`, so the website never bundles them.
- **Unknown types are skipped**: `ArticleLayout` renders nothing for a block type or embed name it does not know (a
  warning in dev), and the admin editor shows "Unknown block type" instead of crashing.
- **Editors may use admin components** (`UiInput`, `EditorTipTapEditor`, `MediaPicker`, ...) in their template - they
  resolve in the admin. They must not import admin files or call admin composables: admin services come from
  `inject(blockEditorServicesKey)` (`blocks/types.ts`, provided by `LayoutEditor`), and the container editor delegates
  to the admin's `ArticleLayoutSectionList`. The website type-checks the editors too.
- **Admin preview = website rendering.** The two `ArticleLayout` copies had diverged; the website's won (section
  styles, the `component` block, i18n, `safeHref` on links and buttons, the larger card/CTA typography), and the admin
  preview now renders the shared component with `preview`, which shows embeds as a labelled placeholder instead of the
  live form.

#### Adding a block

1. Create `layers/shared/app/blocks/<type>/` with `index.ts` (`defineBlock({ type: '<type>', label, order, create })`),
   `Render.vue` and `Editor.vue`. Copy a similar block (e.g. `cta/`) as a start. Text shown on the website goes through
   `useLayerText()` with keys added to `web/i18n/locales/*.json`.
2. Backend: add an entry to `LayoutBlockTypes` (`src/backend/application/Articles/Services/LayoutBlockTypes.cs`):
   its validation rules, how it renders (Markdown through the render context, which sanitises; URLs through
   `UrlSafety.OrNull`) and, if it has text worth indexing, how it flattens to Markdown. A block with fields the layout
   model does not have yet also needs them on `LayoutBlock`, `BlockInputDto`, `RenderedBlockDto`, the copies in
   `LayoutComposer.ParseForAdmin` / `SerializeLayout`, and `types/layout.ts`.
3. Tests: `LayoutBlockRegistryTests` fails until both sides list the block; add validation and rendering cases to
   `ArticleLayoutTests` (`PublishedLayout_WithEveryBlockType_RoundTripsAndRenders` covers every type).

Adding an embed for the component block: create `layers/shared/app/embeds/<Name>/` with `index.ts`
(`defineEmbed({ name: '<Name>', label, component: () => import('./<Name>.vue') })`) and the component, then add
`<Name>` to `LayoutBlockTypes.ComponentNames`.

### Uploaded media (`/uploads/**`)

The API returns image URLs as site-relative paths (`/uploads/2026/11/abc.png`), so both frontends must answer
`/uploads/**` on their own origin. In production Caddy routes `/uploads/*` on the site host to the API before the
request reaches Nuxt. Without Caddy (the dev stack), each app proxies it itself through a server route
(`server/routes/uploads/[...path].ts`) to `NUXT_UPLOADS_PROXY`, read at container start:

- **admin**: always proxies (default `http://localhost:5000`).
- **web**: proxies only when `NUXT_UPLOADS_PROXY` is set (docker-compose.yml sets `http://api:5000`); unset, `/uploads/**`
  answers 404, which is what Caddy-fronted production expects (`deploy/compose.yml` leaves it unset).

### RSS and Atom feeds (web)

The web app serves every feed in both formats from Nitro server routes (`server/routes/**/rss.xml.ts` and
`atom.xml.ts`):

| Feed | RSS 2.0 | Atom 1.0 |
| ---- | ------- | -------- |
| Whole site | `/rss.xml` | `/atom.xml` |
| Category | `/category/{slug}/rss.xml` | `/category/{slug}/atom.xml` |
| Tag | `/tag/{slug}/rss.xml` | `/tag/{slug}/atom.xml` |
| Series | `/series/{slug}/rss.xml` | `/series/{slug}/atom.xml` |

- Each feed holds the newest 20 articles of its scope, newest first. The data comes from the public `/api/v1` list
  endpoints, which read from `public_articles`, so drafts and scheduled articles stay out until they go live.
  `server/utils/feeds.ts` loads the data. `app/utils/feed.ts` builds the XML and is unit-tested.
- Feeds are **summary feeds**: an item has the title, excerpt, publish date, author, category and tags, and a link.
  It never carries the article body. An Atom entry without an excerpt uses its title as the summary, so validators
  don't flag it for missing text.
- **Members-only articles** ("requires sign-in") are included as the same excerpt teaser the website's article lists
  show. The link leads to the article page, where the sign-in prompt takes over. A feed therefore reveals nothing
  that the public lists do not already show.
- Title, description and language come from the site settings. Without a site name, the host name is used. An
  unknown slug answers 404. If the API fails, the route answers 502 instead of an empty feed, so readers don't show
  "no articles". Responses are cacheable for 10 minutes.
- Autodiscovery: every page advertises the site-wide feeds with `<link rel="alternate">` (`plugins/site-head.ts`).
  Category, tag and series pages also advertise their own feeds (`composables/useFeedLinks.ts`).
- Absolute URLs come from the request origin, like `sitemap.xml`. Behind Caddy that relies on `X-Forwarded-Proto`.

### Scheduled articles (admin)

Setting status Published with a future Publish Date schedules an article (see *Public visibility and scheduled
publishing* in [api-design.md](api-design.md)). The admin shows such articles as **Scheduled** (blue badge, from
`isScheduled()` in `app/types/article.ts`) in the article, page and series lists, with the go-live date and time in
the list's date column, and the editor shows "Scheduled: goes live ..." under the Publish Date. The Publish Date input
is a `datetime-local` field in the browser's time zone; the form converts it to and from the API's UTC value.

### Search suggestions (web and admin)

- **Web** (`components/SearchBox.vue`, `composables/useSearchSuggestions.ts`): the header and search-page search box
  is an ARIA 1.2 combobox. From 2 characters it shows suggestions grouped as Articles, Tags, Categories and Series,
  with the matched word starts in bold, plus a final "See all results" option. Arrow keys move, Enter opens the
  highlighted suggestion (or submits to `/search` when none is highlighted), Escape closes. It is still a plain GET
  form, so search works before hydration and without JavaScript. Requests are debounced (150 ms), a newer query
  aborts the one in flight, and answers are cached per tab by query and sign-in state.
- **Admin** (`components/articles/ArticleJumpSearch.vue`, `composables/useArticleSuggestions.ts`): the article list
  search box keeps filtering the list as before and also shows a jump-to list of matching titles in every status,
  with the status badge. Choosing one opens the editor; Enter with nothing highlighted only closes the list.

### TypeScript Frontend

- **Components**: PascalCase (e.g., `ArticleCard.vue`)
- **Composables**: camelCase with `use` prefix (e.g., `useArticles.ts`)
- **Pages**: kebab-case matching routes (e.g., `blog/[slug].vue`)

### Frontend tests

`vitest.config.ts` defines two Vitest projects in both apps, and `npm test` runs both:

- **`unit`** - pure helpers (`app/utils/*`, and in web also the shared `../themes`) in `test/unit/*.test.ts`, plus the
  shared layer's `layers/shared/test/unit/*.test.ts` (sanitizer, suggestion highlighting, featured image sizes). They run
  in plain Node without the Nuxt runtime (time zone pinned to UTC), so a module under test must import what it uses
  explicitly rather than rely on Nuxt auto-imports. `test/tsconfig.json` (referenced from the app's `tsconfig.json`)
  type-checks them. A file that needs a DOM but not Nuxt opts in with `// @vitest-environment happy-dom` - e.g. admin
  `markdownConversion.test.ts`, which runs the editor's whole save path (Markdown -> HTML -> headless TipTap ->
  Markdown) and pins every known lossy construct.
- **`nuxt`** - composables and route middleware in `test/nuxt/*.test.ts`: the session handling (`useWebsiteAuth`; admin
  `useAuth`, `useApi` and the `auth` / `guest` middleware). They run in `@nuxt/test-utils`' `nuxt` environment
  (a client-side Nuxt app on happy-dom), so auto-imports and `import.meta.client` work. `$fetch` is the network
  boundary: tests replace it with `vi.stubGlobal('$fetch', ...)` and mock other auto-imports such as `navigateTo`
  with `mockNuxtImport`. The composables keep per-page-load state (in-flight refresh, init promise, timer) in module
  scope, so each test calls `vi.resetModules()` and imports the module under test dynamically. Nuxt's generated
  `tsconfig.app.json` includes `test/nuxt/**`, so `npm run typecheck` covers them too. The layer's
  `layers/shared/test/nuxt/` holds the block and embed registry tests and the `ArticleLayout` component tests
  (`mountSuspended`).

Both apps run the layer's tests, each with its own dependencies, the same way they build the layer.

Tests live outside `app/` so Nuxt never auto-imports or bundles them.

```bash
cd src/frontend/web      # or src/frontend/admin
npm test                 # vitest run (once); npx vitest for watch mode
npx vitest run test/unit/sanitizeHtml.test.ts   # a single file
npx vitest run --project nuxt                   # only the Nuxt-environment tests
```

CI runs `npm run test:coverage` in the frontend job, after lint and typecheck, and a failing test blocks the PR.
Coverage (V8) covers the TypeScript modules under `app/` and `../layers/shared/app/` (web also `../themes`), not
components or pages; the job
summary shows lines and branches per app and the HTML report is uploaded as an artifact. The run fails when coverage
drops below `coverage.thresholds` in `vitest.config.ts`, which sit just below the current coverage: raise them when
you add tests, never lower them to make a PR pass. Run it locally with `npm run test:coverage` (HTML in
`coverage/index.html`).

### Site identity and language policy

**Branding comes from site settings, not code.** The public website reads these keys from `GET /api/v1/settings`
(edit them in the admin panel under Settings) once per server-side request (`plugins/theme.server.ts`, shared through
`useSiteSettings()`). The head entries built from them (title template, description, `og:site_name`, `<html lang>`)
live in the universal `plugins/site-head.ts`, so the browser keeps the same titles after hydration:

| Setting (admin field) | Used for |
| --------------------- | -------- |
| `site_name` (Site Name) | Page title suffix (`Page | Site name`), `og:site_name`, the default footer line, the header wordmark next to the logo icon (hidden while empty) |
| `site_description` (Site Description) | Default `<meta name="description">` and the home page fallback |
| `language` (Website language) | The `<html lang>` attribute (exact tag, such as `da`, `en`, `en-GB`) and the UI translation (see below) |
| `footer_text` (Footer text) | Footer line; `{year}` is replaced by the current year. Empty shows `© <year> <site name>` |
| `public_email`, `public_phone`, `company_info` (Footer Contact) | The footer "Kontakt" column (mailto/tel links and a free-text line such as a CVR number). All empty shows the generic follow-us text. `contact_email` is separate: the contact form recipient, never shown on the page and not returned by `GET /api/v1/settings` (the public endpoint returns an allow-list, `PublicSiteSettingsDto`) |
| `social_linkedin`, `social_twitter`, `social_facebook`, `social_instagram` (Social Links) | The footer "Følg med" column, shown only when at least one is filled in |

Nothing in the web or admin app may hardcode a person's name, a domain or an organisation. When a setting is empty the
web app falls back to language `da` (`web/app/utils/siteDefaults.ts`) and to the translated `site.defaultTitle` /
`site.defaultDescription` texts (in Danish `Forside` and `Udforsk vores seneste artikler og indsigter`), so a site that
has never filled the settings in keeps its previous look.

**UI language: Danish-first, translatable.** The built-in texts of the public website (buttons, form and error
messages, empty states, page titles and descriptions, breadcrumbs, aria labels) live in `@nuxtjs/i18n` locale files
under `web/i18n/locales/`, never in components: use `const { t } = useI18n()` and `t('section.key')` in components,
`<i18n-t keypath="..." scope="global">` when a sentence contains a link, and `useNuxtApp().$i18n.t` in composables.
Danish (`da.json`) is the default and reference locale; `en.json` ships as well. Content written in the admin
(articles, pages, navigation labels, categories) can be in any language and is not translated.

How the locale is chosen: `plugins/site-locale.ts` maps the `language` setting to a locale file, first by exact code,
then by primary language (`en-GB` uses `en.json` when there is no `en-GB.json`), else `da`. URLs carry no locale
prefix and the visitor's browser language is ignored: a site has one language, chosen by the operator. Dates are
formatted for the active locale (`useLocaleDate()`). `<html lang>` always uses the setting's exact tag.

**Adding a language** needs only a new locale file:

1. Copy `web/i18n/locales/da.json` to `web/i18n/locales/<code>.json`, where `<code>` is a language tag such as `de`
   or `pt-BR`. `nuxt.config.ts` picks up every file in that folder; there is no list to edit.
2. Translate the values and keep the keys. A key missing from the new file falls back to the Danish text. Keep
   `{name}`-style placeholders, the `singular | plural` form of count texts, and write a literal `@` as `{'@'}`
   (vue-i18n message syntax).
3. Rebuild the web image (locale files are bundled at build time) and set "Website language" to `<code>` in Settings.

**Existing sites after upgrading.** A site that previously showed a hardcoded name in the footer should open
Settings once and fill in "Footer text" (for example `© {year} Your Name.`) or "Site Name"; until then the footer reads
just `© <year>`.
