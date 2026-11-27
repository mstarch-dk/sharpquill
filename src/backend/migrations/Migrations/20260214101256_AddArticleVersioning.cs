using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create article_contents table
            migrationBuilder.CreateTable(
                name: "article_contents",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    article_id = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    excerpt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    meta_title = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_contents", x => x.id);
                    table.ForeignKey(
                        name: "FK_article_contents_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_article_contents_article_id_version_number",
                table: "article_contents",
                columns: new[] { "article_id", "version_number" },
                unique: true);

            // 2. Add published_version_id column to articles
            migrationBuilder.AddColumn<int>(
                name: "published_version_id",
                table: "articles",
                type: "integer",
                nullable: true);

            // 3. Migrate existing content into article_contents as version 1
            migrationBuilder.Sql(@"
                INSERT INTO article_contents (article_id, content, excerpt, meta_title, meta_description, version_number, created_at)
                SELECT id, content, excerpt, meta_title, meta_description, 1, created_at
                FROM articles;
            ");

            // 4. Set published_version_id for published and archived articles
            migrationBuilder.Sql(@"
                UPDATE articles a
                SET published_version_id = ac.id
                FROM article_contents ac
                WHERE ac.article_id = a.id AND ac.version_number = 1
                AND a.status IN (1, 2);
            ");

            // 5. Now drop old content columns
            migrationBuilder.DropColumn(
                name: "content",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "excerpt",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "meta_description",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "meta_title",
                table: "articles");

            // 6. Add FK and index for published_version_id
            migrationBuilder.CreateIndex(
                name: "IX_articles_published_version_id",
                table: "articles",
                column: "published_version_id");

            migrationBuilder.AddForeignKey(
                name: "FK_articles_article_contents_published_version_id",
                table: "articles",
                column: "published_version_id",
                principalTable: "article_contents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_articles_article_contents_published_version_id",
                table: "articles");

            // Re-add content columns
            migrationBuilder.AddColumn<string>(
                name: "content",
                table: "articles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "excerpt",
                table: "articles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "meta_description",
                table: "articles",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "meta_title",
                table: "articles",
                type: "character varying(70)",
                maxLength: 70,
                nullable: true);

            // Restore content from latest version
            migrationBuilder.Sql(@"
                UPDATE articles a
                SET content = ac.content,
                    excerpt = ac.excerpt,
                    meta_title = ac.meta_title,
                    meta_description = ac.meta_description
                FROM article_contents ac
                WHERE ac.article_id = a.id
                AND ac.version_number = (
                    SELECT MAX(version_number) FROM article_contents WHERE article_id = a.id
                );
            ");

            migrationBuilder.DropTable(
                name: "article_contents");

            migrationBuilder.DropIndex(
                name: "IX_articles_published_version_id",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "published_version_id",
                table: "articles");
        }
    }
}
