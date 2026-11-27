## API Design

> See also: [backend-code-structure.md](backend-code-structure.md) for controller and service naming, [frontend-uis.md](frontend-uis.md) for how frontends consume these APIs

### URL Structure

- Public API: `/api/v1/{resource}` - used by the public website. Reading is anonymous, except members-only
  articles (see *Website readers* below). Writes (comments, likes, contact, sign-up) are anonymous but rate limited.
- Admin API: `/api/admin/{resource}` - admin-author JWT required (except the login endpoints). In production it is
  only routed on the admin hostname.
- Health: `GET /health` - unauthenticated liveness check used by the container healthchecks.

### Public visibility and scheduled publishing

An article or page is public only when it is **Published and its `publishedAt` has been reached**
(`status = Published AND (published_at IS NULL OR published_at <= now())`, database clock). A Published row with a
future `publishedAt` is *scheduled*: every `/api/v1` endpoint treats it like a draft (404 on its slug, left out of
lists, featured, category/tag/series lists and their article counts, series navigation, navigation links, and comment
creation) and it appears on its own when the time passes - no job and no status change. The admin API is unaffected
and returns scheduled articles with their real status and date. The rule lives in the database view
`public_articles` (see [database-layer.md](database-layer.md)); public repositories read `FROM public_articles a`
instead of `articles`, and every new public query (feeds, search, ...) must do the same. `publishedAt` is stored and returned in UTC.

### Response Format

```json
{
  "data": {},
  "meta": {
    "page": 1,
    "pageSize": 10,
    "totalCount": 100
  }
}
```

### Error Format

```json
{
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "details": [{ "field": "title", "message": "Title is required" }]
  }
}
```

## Authentication

Two separate kinds of users, with separate JWT audiences, refresh-token tables and cookies. A token issued for one
audience is rejected by the other API.

Access tokens are HS256-signed JWTs with the claims `sub` (numeric id), `email`, `name`, `user_type` (`admin` or
`website`), `jti` and, for authors, `role`. The API validates them with the ASP.NET Core JWT bearer handler: one
scheme per audience, each also requiring the matching `user_type`; other algorithms, unsigned or expired tokens and
tokens from another issuer are rejected. Endpoints declare what they need with authorization policies (see
[backend-code-structure.md](backend-code-structure.md#authentication-and-authorization)).

Failed authentication and authorization use the standard error envelope:

| Status | `error.code` | When |
| ------ | ------------ | ---- |
| 401 | `UNAUTHORIZED` | No token, or the token is invalid, expired or for the other audience (`WWW-Authenticate: Bearer`) |
| 403 | `FORBIDDEN` | Signed in, but the role (`Admin role required`) or ownership (`You can only modify your own articles`) does not allow it |

Endpoints that issue tokens answer their own 401 codes (for example `INVALID_CREDENTIALS`, `INVALID_CERTIFICATE`).

### Admin authors (`/api/admin/*`)

- **Login**: `POST /api/admin/auth/login` with email + password, or `POST /api/admin/auth/cert-login` with a client
  certificate (mTLS) - see [admin-security.md](admin-security.md).
- **Access token**: JWT, 15-minute expiry (`Jwt:AccessTokenExpirationMinutes`), sent as `Authorization: Bearer`.
  The admin app keeps it in memory only, never in `localStorage`.
- **Refresh token**: 7-day expiry (`Jwt:RefreshTokenExpirationDays`), `refreshToken` cookie (httpOnly, Secure,
  SameSite=Strict). Stored hashed and rotated on every refresh; replaying an already-rotated token ends all sessions of that author.
- **Roles**: `admin` and `user`. Only admins manage authors and site settings; `user` authors can only edit their
  own articles.
- **Password hashing**: BCrypt with cost factor 12.

### Website readers (`/api/v1/auth/*`)

Optional; switched on and off with the `login_enabled` site setting, which the API enforces (sign-up and login answer
403 while it is off).

1. `POST /api/v1/auth/signup` with name + email creates a **pending** reader. The response is the same whether or not
   the email is already registered.
2. An admin approves the reader in the admin panel and generates a **login link** (`POST
   /api/admin/website-users/{id}/login-token`): a single-use token, valid for 7 days, stored hashed. The admin sends
   the link to the reader.
3. `POST /api/v1/auth/login` with that token redeems it once and returns an access token (website audience) plus a
   `websiteRefreshToken` cookie (httpOnly, Secure, SameSite=Strict).
4. `POST /api/v1/auth/refresh` and `POST /api/v1/auth/logout` work like the admin equivalents;
   `GET /api/v1/auth/profile` returns the signed-in reader.

**Members-only content:** an article with `requiresSignIn` set is returned (and its comments listed or accepted) only
for requests with a valid reader token; anonymous requests get 401.

### Rate limits

Login, refresh and sign-up (`auth`, `auth-refresh`), comments, likes and the contact form have per-client rate limits
(429 when exceeded). The client IP comes from `X-Forwarded-For`, so the API must run behind the reverse proxy.

## Site search (`GET /api/v1/search`)

Full-text search over published articles (not pages), using Postgres full-text search.

- **Query**: `q` (web search syntax: `"exact phrase"`, `or`, `-exclude`; parsed with `websearch_to_tsquery`, so any
  input is valid and a query of only stop words matches nothing), `page`, `pageSize` (max 50). A blank `q` returns an
  empty page. Queries are cut to 200 characters.
- **Visibility**: same rule as the article endpoints: status Published only, and members-only articles appear only
  for requests with a website-reader token.
- **Ranking**: `ts_rank` over title (weight A), excerpt (B) and body text (C); ties go to the newest article.
- **Response**: the standard envelope with pagination `meta`. Each item is `{ article, snippet }`: `article` has the
  same shape as an item of `GET /api/v1/articles`; `snippet` is a list of `{ text, match }` runs built with
  `ts_headline`, where `match: true` marks matched words. It is plain text, so clients highlight matches without
  rendering HTML from the API.

The index (`articles.search_vector`) is maintained in the database, see
[database-layer.md](database-layer.md#full-text-search).

## Search suggestions (`GET /api/v1/search/suggest`, `GET /api/admin/articles/suggest`)

Search-as-you-type suggestions, answered from the API's in-memory suggestion index (no database query per request;
see [backend-code-structure.md](backend-code-structure.md#search-suggestion-index)).

- **Matching**: word prefix ("right truncation") on titles and names. Every word of `q` must be the start of some word,
  in any order: `post gre` finds "PostgreSQL is great". Case-insensitive; diacritics count (`å` does not match `a`).
  Fewer than 2 letters/digits, or only symbols, return an empty list; at most 5 words are used.
- **Ranking**: titles that start with the query as typed first, then earlier word positions, then the newest article.
- **Public** (`/api/v1/search/suggest?q=`): up to 6 articles (same visibility as site search: Published, publish date
  reached, members-only only for a website-reader token), then up to 2 each of tags, categories and series that have
  at least one public article. Items are `{ type, title, url }` with `type` `article` / `tag` / `category` / `series`
  and `url` the website path (`/blog/{slug}`, `/tag/{slug}`, `/category/{slug}`, `/series/{slug}`).
- **Admin** (`/api/admin/articles/suggest?q=&limit=`, `AdminUser`): articles (not pages) in every status, `limit`
  1-20 (default 8). Items are `{ id, title, status, publishedAt }`.
- Changes show within about a second of the commit.

## Admin article list (`GET /api/admin/articles`)

Lists articles of every status (pages have their own `GET /api/admin/pages`). All filters are optional and combine
(every set filter must match); the response is the standard envelope with pagination `meta`.

- `search`: web search syntax over title, excerpt and body of the article's **latest** version (so drafts,
  unpublished edits and archived articles are found by what the author last wrote), or the text anywhere in the
  title, so a half-typed word still finds it. Cut to 200 characters.
- `status`: `draft`, `published` (live), `scheduled` (published with a future date) or `archived`.
- `categoryId`, `authorId`: numeric ids.
- `sort`: `relevance` (default with a `search`; title matches first), `newest` (default otherwise, by creation date),
  `oldest`, `updated` or `title` (A-Z, case-insensitive).
- `page`, `pageSize` (max 50).

An unknown `status` or `sort` answers 400 `VALIDATION_ERROR`. The admin article list keeps these parameters in its
own URL query, so reload, links and back/forward restore the list.

## Admin media (`/api/admin/media`)

Uploads are stored under `Uploads__Path` as `{year}/{month}/{guid}.{ext}`, with `_medium` and `_thumb` variants for
raster images. The admin manages them on its **Media** page (`/media`).

- `GET /api/admin/media/{id}/usages`: where the file is still used. Each entry has a `kind`, the `articleId` and
  `isPage` for article/page usages, and a `title` (article or page title, setting key or author name). Kinds:
  - `featured_image`: an article's or page's featured image (`articles.featured_image_id`).
  - `content`: its URL (original or a variant) in the Markdown of the **latest or the published** version.
  - `layout`: a layout block of the latest or the published version (`mediaId` of an image block, or its URL).
  - `setting`: its URL in a site setting value; `author_avatar`: its URL as an author avatar.

  Older versions are history and do not count: restoring one whose image was deleted shows a broken image.
- `DELETE /api/admin/media/{id}`: deletes the record and the original, medium and thumbnail files. While the file is
  still used it answers **409 `MEDIA_IN_USE`** (the usages as error `details`: `field` = kind, `message` = title) and
  deletes nothing. `?force=true` deletes anyway: featured images are cleared (the foreign key is `ON DELETE SET NULL`)
  and inline references are left broken. The admin shows the usages in the delete dialog and forces only after the
  author confirms "Delete anyway".
