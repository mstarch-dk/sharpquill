## Backend Code Structure

> See also: [project-structure.md](project-structure.md) for directory layout, [database-layer.md](database-layer.md) for SQL conventions and repository implementation, [api-design.md](api-design.md) for controller response formats

## Project Dependencies

```
api → application → core
migrations → core (standalone for migrations
```

## Naming Conventions

### C# Backend

- **Entities**: PascalCase, singular (e.g., `Article`, `Category`)
- **DTOs**: `{Entity}Dto`, `{Entity}CreateDto`, `{Entity}UpdateDto`
- **Repositories**: `I{Entity}Repository` / `{Entity}Repository`
- **Services**: `I{Entity}Service` / `{Entity}Service`
- **Controllers**: `{Entity}sController` (plural)

### Service Layer

- Services in SharpQuill.Application handle business logic
- Map between entities and DTOs
- Orchestrate repository calls

### Search suggestion index

Search-as-you-type suggestions (`GET /api/v1/search/suggest`, `GET /api/admin/articles/suggest`) never query the
database per keystroke. They are answered from an in-memory index:

- `core/Articles/Suggestions/SuggestionIndex` - an immutable index of article titles and tag, category and series names.
  All words are kept in one ordinal-sorted array; a query word is a binary search for its prefix range. Visibility
  (status, publish date, members-only) is checked per query, so scheduled articles appear on time without a signal.
  `Apply(delta)` returns a new index; readers never lock.
- `application/Articles/Suggestions/SuggestionIndexHolder` (singleton) - the current index, swapped atomically.
- `application/Articles/Repositories/SuggestionIndexRepository` - full load, and load-by-ids for deltas.
- `api/Infrastructure/SuggestionIndexListener` (hosted service) - LISTENs on `cms_suggest` (see
  [database-layer.md](database-layer.md#search-suggestion-notifications-cms_suggest)) *before* the full load at
  startup, coalesces notifications for 250 ms, reloads only the named rows and swaps in the new index. It reconnects
  with backoff and then reloads everything (notifications sent while disconnected are lost), and reloads everything
  every 15 minutes as a safety net. Until the first load finishes the endpoints answer an empty list.

Each API instance holds its own index; NOTIFY reaches all of them, so several instances stay in sync. Memory use is
small (a few hundred bytes per title).

## Authentication and Authorization

The API uses the standard ASP.NET Core pipeline (`UseAuthentication()` / `UseAuthorization()`); everything is wired
in `api/Infrastructure/Auth/AuthenticationSetup.cs` (`AddCmsAuthentication`).

- **Schemes**: two JWT bearer schemes, `AdminJwt` and `WebsiteJwt`. Each validates signature (HS256 only, signed
  tokens required), issuer, its own audience and lifetime (no clock skew), and then requires `user_type` to match
  (`admin` / `website`) and `sub` to be a numeric id. Claim names are kept as issued (`MapInboundClaims = false`).
- **Default scheme** `Bearer` is a policy scheme that forwards to one of the two by the token's audience, so
  endpoints without a policy (public endpoints that only behave differently for signed-in readers) still see the
  caller in `User`. It never grants access by itself; the forwarded scheme does the full validation.
- **Policies** (`AuthPolicies`), applied with `[Authorize(Policy = ...)]` on the controller or action:
  - `AdminUser` - any author (admin scheme only). Used on every `/api/admin/*` controller except the login endpoints.
  - `AdminRole` - author with role `admin` (authors, website users, import/export, settings updates). A missing role
    claim never counts as admin.
  - `WebsiteUser` - signed-in reader (website scheme only), e.g. `GET /api/v1/auth/profile`.
- **Resource-based rules**: article ownership is `ArticleOwnershipHandler` (`ModifyArticleRequirement` on an
  `ArticleAdminDto`): admins may modify any article, other authors only their own. Controllers call
  `IAuthorizationService.AuthorizeAsync(User, article, ModifyArticleRequirement.Instance)`.
- **Caller identity**: read it with the `ClaimsPrincipalExtensions` on `User` (`GetAuthorId()`,
  `GetWebsiteUserId()`, `IsAdminRole()`, `IsWebsiteUser()`), never from `HttpContext.Items` or raw claims.
- **Failures**: `ApiAuthorizationResultHandler` answers 401 (`UNAUTHORIZED`, with `WWW-Authenticate: Bearer`) and
  403 (`FORBIDDEN`) in the standard error envelope. Rejected tokens are logged as warnings (category
  `SharpQuill.Api.Auth`: scheme, path and reason, never the token).
