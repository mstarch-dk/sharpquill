using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <summary>
    /// public_articles: the articles and pages the public site may show. A row is public when it is Published (1)
    /// and its publish date has been reached; a Published row with a future published_at is "scheduled" and appears
    /// on its own once now() passes it. A NULL published_at counts as already published. Every public (api/v1)
    /// query reads this view instead of the articles table, so the rule lives in one place.
    ///
    /// Postgres expands "a.*" when the view is created, so a later migration that adds a column to articles must
    /// re-run the CREATE OR REPLACE VIEW below (new columns are appended, which CREATE OR REPLACE allows).
    /// PublicArticlesViewTests fails when the two column lists drift apart.
    /// </summary>
    public partial class AddPublicArticlesView : Migration
    {
        internal const string CreateViewSql = @"
            CREATE OR REPLACE VIEW public_articles AS
            SELECT a.*
            FROM articles a
            WHERE a.status = 1
              AND (a.published_at IS NULL OR a.published_at <= now());";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CreateViewSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS public_articles;");
        }
    }
}
