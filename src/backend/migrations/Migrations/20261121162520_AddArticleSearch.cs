using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <summary>
    /// Public site search (Postgres full-text search). Adds <c>articles.search_vector</c> with a GIN index and the SQL
    /// functions and triggers that keep it current: title (weight A), excerpt (B) and body text (C) of the published
    /// version, stemmed with the text search configuration for the site's <c>language</c> setting.
    /// <para>
    /// A STORED generated column cannot do this: the body lives in <c>article_contents</c> and the configuration is a
    /// runtime setting, so triggers maintain the column on every write path, and a change of the language setting
    /// rebuilds all vectors. The column is not part of the EF model (EF is used for migrations only and the runtime
    /// reads it with raw SQL), so later migrations leave it alone.
    /// </para>
    /// </summary>
    public partial class AddArticleSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            -- Text search configuration for the site's `language` setting ("da", "en-GB", ...): primary subtag mapped to a
            -- built-in Snowball configuration. An empty setting counts as Danish, the web app's default language; a language
            -- without a stemmer gets 'simple' (lower-cased words, no stemming, no stop words).
            CREATE FUNCTION cms_search_config() RETURNS regconfig
            LANGUAGE plpgsql STABLE AS $fn$
            DECLARE
                lang text;
                cfg text;
            BEGIN
                SELECT lower(split_part(replace(trim(value), '_', '-'), '-', 1)) INTO lang
                FROM site_settings WHERE key = 'language';

                cfg := CASE coalesce(lang, '')
                    WHEN '' THEN 'danish'
                    WHEN 'da' THEN 'danish'
                    WHEN 'en' THEN 'english'
                    WHEN 'de' THEN 'german'
                    WHEN 'sv' THEN 'swedish'
                    WHEN 'nb' THEN 'norwegian'
                    WHEN 'nn' THEN 'norwegian'
                    WHEN 'no' THEN 'norwegian'
                    WHEN 'fi' THEN 'finnish'
                    WHEN 'nl' THEN 'dutch'
                    WHEN 'fr' THEN 'french'
                    WHEN 'es' THEN 'spanish'
                    WHEN 'it' THEN 'italian'
                    WHEN 'pt' THEN 'portuguese'
                    ELSE 'simple'
                END;

                IF NOT EXISTS (SELECT 1 FROM pg_ts_config WHERE cfgname = cfg) THEN
                    cfg := 'simple';
                END IF;

                RETURN cfg::regconfig;
            END;
            $fn$;

            -- Markdown to plain text, good enough for indexing and snippets: keeps link and image texts, drops URLs, HTML tags,
            -- line markers (headings, quotes, lists) and emphasis/code markers. Characters 0x01-0x08 are removed because the
            -- search query uses them as highlight markers.
            CREATE FUNCTION cms_markdown_to_text(md text) RETURNS text
            LANGUAGE sql IMMUTABLE PARALLEL SAFE AS $fn$
                SELECT trim(regexp_replace(
                    regexp_replace(
                    regexp_replace(
                    regexp_replace(
                    regexp_replace(
                    regexp_replace(
                    regexp_replace(coalesce(md, ''),
                        '[\x01-\x08]', '', 'g'),
                        '!?\[([^\]]*)\]\([^)]*\)', '\1', 'g'),
                        '<[^>]*>', ' ', 'g'),
                        '^[ \t]{0,3}(#{1,6}|>+|[-*+]|[0-9]+[.)])[ \t]+', '', 'gn'),
                        '(\*\*|__|~~|`+|\*)', '', 'g'),
                        '&(nbsp|amp|lt|gt|quot);', ' ', 'g'),
                    '\s+', ' ', 'g'));
            $fn$;

            -- Readable text of a block layout: the text fields of every block and card/step item, in document order.
            CREATE FUNCTION cms_layout_to_text(layout jsonb) RETURNS text
            LANGUAGE sql IMMUTABLE PARALLEL SAFE AS $fn$
                SELECT coalesce(string_agg(cms_markdown_to_text(t.value), ' ' ORDER BY o.ord, t.ord), '')
                FROM jsonb_path_query(coalesce(layout, 'null'::jsonb), 'strict $.** ? (@.type() == "object")')
                     WITH ORDINALITY AS o(obj, ord)
                CROSS JOIN LATERAL (
                    VALUES (1, o.obj->>'heading'), (2, o.obj->>'title'), (3, o.obj->>'content'),
                           (4, o.obj->>'description'), (5, o.obj->>'caption'), (6, o.obj->>'buttonText')
                ) AS t(ord, value)
                WHERE t.value IS NOT NULL AND t.value <> '' AND coalesce(o.obj->>'type', '') NOT IN ('component', 'embed');
            $fn$;

            -- Body text of one article version (Markdown content or block layout).
            CREATE FUNCTION cms_article_body_text(content text, layout jsonb) RETURNS text
            LANGUAGE sql IMMUTABLE PARALLEL SAFE AS $fn$
                SELECT trim(cms_markdown_to_text(content) || ' ' || cms_layout_to_text(layout));
            $fn$;

            -- Weighted search document: title (A), excerpt (B), body (C) of the published version.
            CREATE FUNCTION cms_article_search_vector(p_title text, p_version_id integer) RETURNS tsvector
            LANGUAGE sql STABLE AS $fn$
                SELECT setweight(to_tsvector(c.cfg, coalesce(p_title, '')), 'A')
                    || setweight(to_tsvector(c.cfg, coalesce(ac.excerpt, '')), 'B')
                    || setweight(to_tsvector(c.cfg, coalesce(cms_article_body_text(ac.content, ac.layout), '')), 'C')
                FROM (SELECT cms_search_config() AS cfg) AS c
                LEFT JOIN article_contents ac ON ac.id = p_version_id;
            $fn$;

            ALTER TABLE articles ADD COLUMN search_vector tsvector;
            CREATE INDEX ix_articles_search_vector ON articles USING gin (search_vector);

            -- Keep the vector in step with every write path (admin editor, import, publishing, scheduled jobs).
            CREATE FUNCTION cms_articles_search_vector_trigger() RETURNS trigger
            LANGUAGE plpgsql AS $fn$
            BEGIN
                NEW.search_vector := cms_article_search_vector(NEW.title, NEW.published_version_id);
                RETURN NEW;
            END;
            $fn$;

            CREATE TRIGGER trg_articles_search_vector
                BEFORE INSERT OR UPDATE OF title, published_version_id ON articles
                FOR EACH ROW EXECUTE FUNCTION cms_articles_search_vector_trigger();

            -- Article versions are written once, but an in-place edit of a published version must not leave a stale vector.
            CREATE FUNCTION cms_article_contents_search_vector_trigger() RETURNS trigger
            LANGUAGE plpgsql AS $fn$
            BEGIN
                UPDATE articles SET search_vector = cms_article_search_vector(title, published_version_id)
                WHERE published_version_id = NEW.id;
                RETURN NULL;
            END;
            $fn$;

            CREATE TRIGGER trg_article_contents_search_vector
                AFTER UPDATE OF content, layout, excerpt ON article_contents
                FOR EACH ROW EXECUTE FUNCTION cms_article_contents_search_vector_trigger();

            -- A new website language changes stemming and stop words: rebuild every vector.
            CREATE FUNCTION cms_site_settings_search_language_trigger() RETURNS trigger
            LANGUAGE plpgsql AS $fn$
            BEGIN
                IF (TG_OP = 'DELETE' AND OLD.key = 'language')
                   OR (TG_OP = 'INSERT' AND NEW.key = 'language')
                   OR (TG_OP = 'UPDATE' AND (NEW.key = 'language' OR OLD.key = 'language')
                       AND (NEW.key IS DISTINCT FROM OLD.key OR NEW.value IS DISTINCT FROM OLD.value)) THEN
                    UPDATE articles SET search_vector = cms_article_search_vector(title, published_version_id);
                END IF;
                RETURN NULL;
            END;
            $fn$;

            CREATE TRIGGER trg_site_settings_search_language
                AFTER INSERT OR UPDATE OR DELETE ON site_settings
                FOR EACH ROW EXECUTE FUNCTION cms_site_settings_search_language_trigger();

            UPDATE articles SET search_vector = cms_article_search_vector(title, published_version_id);
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            DROP TRIGGER IF EXISTS trg_site_settings_search_language ON site_settings;
            DROP TRIGGER IF EXISTS trg_article_contents_search_vector ON article_contents;
            DROP TRIGGER IF EXISTS trg_articles_search_vector ON articles;
            DROP FUNCTION IF EXISTS cms_site_settings_search_language_trigger();
            DROP FUNCTION IF EXISTS cms_article_contents_search_vector_trigger();
            DROP FUNCTION IF EXISTS cms_articles_search_vector_trigger();
            DROP INDEX IF EXISTS ix_articles_search_vector;
            ALTER TABLE articles DROP COLUMN IF EXISTS search_vector;
            DROP FUNCTION IF EXISTS cms_article_search_vector(text, integer);
            DROP FUNCTION IF EXISTS cms_article_body_text(text, jsonb);
            DROP FUNCTION IF EXISTS cms_layout_to_text(jsonb);
            DROP FUNCTION IF EXISTS cms_markdown_to_text(text);
            DROP FUNCTION IF EXISTS cms_search_config();
            """);
        }
    }
}
