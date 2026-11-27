using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <summary>
    /// Admin article list search. Adds <c>articles.admin_search_vector</c> with a GIN index: title (weight A), excerpt (B)
    /// and body text (C) of the <em>latest</em> version, so drafts, unpublished edits and archived articles are found by
    /// what the author last wrote. <c>search_vector</c> (AddArticleSearch) covers only the published version and stays
    /// the public index.
    /// <para>
    /// Triggers keep the column current, reusing the text functions of AddArticleSearch; the language-setting trigger is
    /// replaced so a new language rebuilds both vectors. Like <c>search_vector</c> the column is not in the EF model.
    /// </para>
    /// </summary>
    public partial class AddAdminArticleSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            -- Weighted search document of an article's latest version: title (A), excerpt (B), body (C).
            CREATE FUNCTION cms_article_admin_search_vector(p_article_id integer, p_title text) RETURNS tsvector
            LANGUAGE sql STABLE AS $fn$
                SELECT setweight(to_tsvector(c.cfg, coalesce(p_title, '')), 'A')
                    || setweight(to_tsvector(c.cfg, coalesce(lv.excerpt, '')), 'B')
                    || setweight(to_tsvector(c.cfg, coalesce(cms_article_body_text(lv.content, lv.layout), '')), 'C')
                FROM (SELECT cms_search_config() AS cfg) AS c
                LEFT JOIN LATERAL (
                    SELECT ac.excerpt, ac.content, ac.layout
                    FROM article_contents ac
                    WHERE ac.article_id = p_article_id
                    ORDER BY ac.version_number DESC
                    LIMIT 1
                ) AS lv ON true;
            $fn$;

            ALTER TABLE articles ADD COLUMN admin_search_vector tsvector;
            CREATE INDEX ix_articles_admin_search_vector ON articles USING gin (admin_search_vector);

            -- New article or renamed article. On insert the row has no versions yet, so this is the title only; the
            -- article_contents trigger below adds the body when the first version is written.
            CREATE FUNCTION cms_articles_admin_search_vector_trigger() RETURNS trigger
            LANGUAGE plpgsql AS $fn$
            BEGIN
                NEW.admin_search_vector := cms_article_admin_search_vector(NEW.id, NEW.title);
                RETURN NEW;
            END;
            $fn$;

            CREATE TRIGGER trg_articles_admin_search_vector
                BEFORE INSERT OR UPDATE OF title ON articles
                FOR EACH ROW EXECUTE FUNCTION cms_articles_admin_search_vector_trigger();

            -- Every saved, edited or removed version can change which text is the latest.
            CREATE FUNCTION cms_article_contents_admin_search_vector_trigger() RETURNS trigger
            LANGUAGE plpgsql AS $fn$
            DECLARE
                v_article_id integer := CASE WHEN TG_OP = 'DELETE' THEN OLD.article_id ELSE NEW.article_id END;
            BEGIN
                UPDATE articles SET admin_search_vector = cms_article_admin_search_vector(id, title)
                WHERE id = v_article_id;
                RETURN NULL;
            END;
            $fn$;

            CREATE TRIGGER trg_article_contents_admin_search_vector
                AFTER INSERT OR DELETE OR UPDATE OF content, layout, excerpt, version_number ON article_contents
                FOR EACH ROW EXECUTE FUNCTION cms_article_contents_admin_search_vector_trigger();

            -- A new website language changes stemming and stop words: rebuild both vectors.
            CREATE OR REPLACE FUNCTION cms_site_settings_search_language_trigger() RETURNS trigger
            LANGUAGE plpgsql AS $fn$
            BEGIN
                IF (TG_OP = 'DELETE' AND OLD.key = 'language')
                   OR (TG_OP = 'INSERT' AND NEW.key = 'language')
                   OR (TG_OP = 'UPDATE' AND (NEW.key = 'language' OR OLD.key = 'language')
                       AND (NEW.key IS DISTINCT FROM OLD.key OR NEW.value IS DISTINCT FROM OLD.value)) THEN
                    UPDATE articles SET
                        search_vector = cms_article_search_vector(title, published_version_id),
                        admin_search_vector = cms_article_admin_search_vector(id, title);
                END IF;
                RETURN NULL;
            END;
            $fn$;

            UPDATE articles SET admin_search_vector = cms_article_admin_search_vector(id, title);
            """);

            // public_articles is SELECT a.*, expanded when the view is created: re-create it so it has the new column
            // too (PublicArticlesViewTests guards that the view and the table keep the same columns).
            migrationBuilder.Sql(AddPublicArticlesView.CreateViewSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION cms_site_settings_search_language_trigger() RETURNS trigger
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

            DROP TRIGGER IF EXISTS trg_article_contents_admin_search_vector ON article_contents;
            DROP TRIGGER IF EXISTS trg_articles_admin_search_vector ON articles;
            DROP FUNCTION IF EXISTS cms_article_contents_admin_search_vector_trigger();
            DROP FUNCTION IF EXISTS cms_articles_admin_search_vector_trigger();
            DROP INDEX IF EXISTS ix_articles_admin_search_vector;
            -- The view depends on the column: drop it, drop the column, then re-create it without the column.
            DROP VIEW IF EXISTS public_articles;
            ALTER TABLE articles DROP COLUMN IF EXISTS admin_search_vector;
            DROP FUNCTION IF EXISTS cms_article_admin_search_vector(integer, text);
            """);
            migrationBuilder.Sql(AddPublicArticlesView.CreateViewSql);
        }
    }
}
