## Database Layer

> See also: [backend-code-structure.md](backend-code-structure.md) for repository interfaces and naming conventions, [project-structure.md](project-structure.md) for migrations project location

### Database

- **Tables**: snake_case, plural (e.g., `articles`, `categories`)
- **Columns**: snake_case (e.g., `published_at`, `meta_title`)
- **Primary keys**: `id` (integer, auto-increment)
- **Foreign keys**: `{entity}_id` (e.g., `author_id`, `category_id`)
- **Join tables**: `{entity1}_{entity2}` (e.g., `article_tags`)

### Views

- **`public_articles`**: the articles and pages the public site may show - `SELECT a.* FROM articles a` filtered to
  Published rows whose `published_at` is NULL or `<= now()` (scheduled publishing). Public (`/api/v1`) repositories
  read this view instead of `articles`, so the visibility rule is written once; admin repositories keep using
  `articles`. Postgres freezes `a.*` when a view is created, so **a migration that adds or changes `articles` columns
  must re-run the `CREATE OR REPLACE VIEW`** from migration `AddPublicArticlesView` (`CreateViewSql`).
  `PublicArticlesViewTests` fails when the column lists drift apart.

## Development Principles

- EF Core is used strictly for schema migrations. All runtime data access uses Npgsql with direct SQL queries. Never introduce EF Core for runtime queries.
- Database schema changes must go through `dotnet ef migrations add` in the migrations project. Never modify the database schema outside of migrations.

## Data Access Patterns

### Why Separate Migrations Project?

- EF Core is used **only** for schema migrations (`dotnet ef migrations add`)
- All runtime queries use Npgsql with raw SQL
- Rationale: Performance, full control over queries, no EF overhead

### Repository Pattern

```csharp
// Interface in SharpQuill.Core.Articles
public interface IArticleRepository
{
    Task<Article?> GetBySlugAsync(string slug);
    Task<IEnumerable<Article>> GetPublishedAsync(int page, int pageSize);
}

// Implementation in SharpQuill.Application.Articles.Repositories uses Npgsql
public class ArticleRepository : IArticleRepository
{
    private readonly NpgsqlDataSource _dataSource;
    // Raw SQL queries here
}
```

## Full-text search

Site search (`GET /api/v1/search`) reads `articles.search_vector` (GIN index), which the migration
`AddArticleSearch` adds together with the SQL functions and triggers that keep it current. Runtime code only reads
it; no repository writes it.

- **Content**: title (weight A), excerpt (B) and body text (C) of the *published* version. The body is the Markdown
  `content` turned into plain text (`cms_markdown_to_text`: link and image texts kept, URLs and markup dropped) plus
  the text fields of a block `layout` (`cms_layout_to_text`).
- **Language**: `cms_search_config()` maps the `language` setting to a Postgres text search configuration by primary
  subtag (`da` -> `danish`, `en`/`en-GB` -> `english`, also `de`, `sv`, `nb`/`nn`/`no`, `fi`, `nl`, `fr`, `es`, `it`,
  `pt`). An empty setting counts as Danish (the web app's default); other languages use `simple` (no stemming).
- **Triggers**: `articles` (insert, or update of `title` / `published_version_id`) rebuilds the row's vector;
  an update of a published `article_contents` row rebuilds the article's vector; inserting, changing or deleting the
  `language` setting rebuilds every vector. So every write path (editor, import, publishing jobs) stays in sync.
- **Why not a generated column**: a STORED generated column can only read its own row and needs a constant text
  search configuration, while the body lives in `article_contents` and the language is a runtime setting.
- The column is not part of the EF model (EF is migrations-only), so later migrations leave it alone. A migration
  that renames `title`, `published_version_id`, `content`, `excerpt` or `layout` must update these functions.
- **Admin index**: `articles.admin_search_vector` (GIN index, migration `AddAdminArticleSearch`) holds the same
  weighted document for the article's *latest* version (highest `version_number`), for the admin article list search.
  Triggers rebuild it when an article is inserted or its `title` changes and when a version is inserted, edited or
  deleted; the `language` setting trigger rebuilds both vectors. Public queries never read it.

## Search suggestion notifications (`cms_suggest`)

Search-as-you-type suggestions are answered from an in-memory index in the API (see
[backend-code-structure.md](backend-code-structure.md#search-suggestion-index)), so the database only has to say
*what* changed. Migration `AddSuggestionNotify` adds `cms_notify_suggest()` and AFTER ROW triggers that send
`NOTIFY cms_suggest, '<kind>:<id>'`:

- `articles`: insert, delete, or an update of `title`, `slug`, `status`, `published_at`, `requires_sign_in`,
  `content_type`, `category_id`, `series_id` -> `article:<id>`. Search-vector rewrites send nothing.
- `article_tags`: insert, update, delete -> `article:<article_id>`.
- `tags`, `categories`, `series`: insert, delete, or an update of `name` / `slug` -> `tag:<id>`, `category:<id>`,
  `series:<id>`.
- `TRUNCATE` of any of these tables -> `reload:0` (full reload).

NOTIFY is transactional (delivered on commit only; equal payloads in one transaction arrive once), so a rolled-back
write sends nothing. A migration that renames one of these columns, or adds a column the index should hold, must update
the triggers and `SuggestionIndexRepository`. Scheduled publishing needs no notification: the index keeps
`published_at` and compares it with the clock when it answers.
