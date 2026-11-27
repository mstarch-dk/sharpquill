using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <summary>
    /// Change signals for the API's in-memory search-suggestion index. Triggers on articles, article_tags, tags,
    /// categories and series send <c>NOTIFY cms_suggest, '&lt;kind&gt;:&lt;id&gt;'</c> (kind: article, tag, category,
    /// series), and a TRUNCATE sends <c>'reload:0'</c>. NOTIFY is transactional: it is delivered on commit only, and
    /// identical payloads within one transaction are delivered once.
    /// <para>
    /// The article trigger fires only for the columns the index holds, so rewriting the search vectors (for example
    /// after a change of the language setting) sends nothing. Scheduled publishing needs no signal: the index keeps
    /// published_at and checks it when it answers.
    /// </para>
    /// </summary>
    public partial class AddSuggestionNotify : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            CREATE FUNCTION cms_notify_suggest() RETURNS trigger
            LANGUAGE plpgsql AS $fn$
            -- TG_ARGV[0]: payload kind; TG_ARGV[1]: name of the id column ('id', or 'article_id' for article_tags).
            -- An UPDATE notifies the old and the new id (an article_tags row can move to another article); equal
            -- payloads are merged by Postgres.
            BEGIN
                IF TG_OP = 'TRUNCATE' THEN
                    PERFORM pg_notify('cms_suggest', 'reload:0');
                    RETURN NULL;
                END IF;
                IF TG_OP IN ('INSERT', 'UPDATE') THEN
                    PERFORM pg_notify('cms_suggest', TG_ARGV[0] || ':' || (to_jsonb(NEW) ->> TG_ARGV[1]));
                END IF;
                IF TG_OP IN ('DELETE', 'UPDATE') THEN
                    PERFORM pg_notify('cms_suggest', TG_ARGV[0] || ':' || (to_jsonb(OLD) ->> TG_ARGV[1]));
                END IF;
                RETURN NULL;
            END;
            $fn$;

            CREATE TRIGGER trg_articles_notify_suggest
                AFTER INSERT OR DELETE OR UPDATE OF title, slug, status, published_at, requires_sign_in, content_type,
                    category_id, series_id ON articles
                FOR EACH ROW EXECUTE FUNCTION cms_notify_suggest('article', 'id');

            CREATE TRIGGER trg_article_tags_notify_suggest
                AFTER INSERT OR DELETE OR UPDATE ON article_tags
                FOR EACH ROW EXECUTE FUNCTION cms_notify_suggest('article', 'article_id');

            CREATE TRIGGER trg_tags_notify_suggest
                AFTER INSERT OR DELETE OR UPDATE OF name, slug ON tags
                FOR EACH ROW EXECUTE FUNCTION cms_notify_suggest('tag', 'id');

            CREATE TRIGGER trg_categories_notify_suggest
                AFTER INSERT OR DELETE OR UPDATE OF name, slug ON categories
                FOR EACH ROW EXECUTE FUNCTION cms_notify_suggest('category', 'id');

            CREATE TRIGGER trg_series_notify_suggest
                AFTER INSERT OR DELETE OR UPDATE OF name, slug ON series
                FOR EACH ROW EXECUTE FUNCTION cms_notify_suggest('series', 'id');

            CREATE TRIGGER trg_articles_truncate_notify_suggest
                AFTER TRUNCATE ON articles FOR EACH STATEMENT EXECUTE FUNCTION cms_notify_suggest('reload', 'id');
            CREATE TRIGGER trg_article_tags_truncate_notify_suggest
                AFTER TRUNCATE ON article_tags FOR EACH STATEMENT EXECUTE FUNCTION cms_notify_suggest('reload', 'id');
            CREATE TRIGGER trg_tags_truncate_notify_suggest
                AFTER TRUNCATE ON tags FOR EACH STATEMENT EXECUTE FUNCTION cms_notify_suggest('reload', 'id');
            CREATE TRIGGER trg_categories_truncate_notify_suggest
                AFTER TRUNCATE ON categories FOR EACH STATEMENT EXECUTE FUNCTION cms_notify_suggest('reload', 'id');
            CREATE TRIGGER trg_series_truncate_notify_suggest
                AFTER TRUNCATE ON series FOR EACH STATEMENT EXECUTE FUNCTION cms_notify_suggest('reload', 'id');
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_series_truncate_notify_suggest ON series;
            DROP TRIGGER IF EXISTS trg_categories_truncate_notify_suggest ON categories;
            DROP TRIGGER IF EXISTS trg_tags_truncate_notify_suggest ON tags;
            DROP TRIGGER IF EXISTS trg_article_tags_truncate_notify_suggest ON article_tags;
            DROP TRIGGER IF EXISTS trg_articles_truncate_notify_suggest ON articles;
            DROP TRIGGER IF EXISTS trg_series_notify_suggest ON series;
            DROP TRIGGER IF EXISTS trg_categories_notify_suggest ON categories;
            DROP TRIGGER IF EXISTS trg_tags_notify_suggest ON tags;
            DROP TRIGGER IF EXISTS trg_article_tags_notify_suggest ON article_tags;
            DROP TRIGGER IF EXISTS trg_articles_notify_suggest ON articles;
            DROP FUNCTION IF EXISTS cms_notify_suggest();
            """);
        }
    }
}
