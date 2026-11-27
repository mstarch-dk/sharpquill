using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <summary>
    /// Re-creates public_articles so it includes articles.search_vector. AddArticleSearch (20261121162520) and
    /// AddPublicArticlesView (20261121164742) were built on parallel branches: a fresh database runs them in that
    /// order and the view already has the column, but a database that applied AddPublicArticlesView first got
    /// AddArticleSearch afterwards and its view lacks search_vector. CREATE OR REPLACE VIEW may append columns, so
    /// this brings both kinds of database to the same view and is a no-op where it already matches.
    /// </summary>
    public partial class RefreshPublicArticlesView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(AddPublicArticlesView.CreateViewSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to undo: the view keeps the columns articles has; AddPublicArticlesView's Down drops it.
        }
    }
}
