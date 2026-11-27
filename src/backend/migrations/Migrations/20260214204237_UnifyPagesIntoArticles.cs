using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class UnifyPagesIntoArticles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add content_type column to articles (default 0 = Article)
            migrationBuilder.AddColumn<int>(
                name: "content_type",
                table: "articles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_articles_content_type",
                table: "articles",
                column: "content_type");

            // Step 2: Make author_id nullable
            migrationBuilder.DropForeignKey(
                name: "f_k_articles__authors_author_id",
                table: "articles");

            migrationBuilder.AlterColumn<int>(
                name: "author_id",
                table: "articles",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "f_k_articles__authors_author_id",
                table: "articles",
                column: "author_id",
                principalTable: "authors",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Step 3: Migrate existing pages into articles
            // Handle slug collisions by appending '-page' to conflicting page slugs
            migrationBuilder.Sql(@"
                INSERT INTO articles (title, slug, published_at, created_at, updated_at, status, content_type, featured_image_id)
                SELECT
                    p.title,
                    CASE
                        WHEN EXISTS (SELECT 1 FROM articles a WHERE a.slug = p.slug)
                        THEN p.slug || '-page'
                        ELSE p.slug
                    END,
                    CASE WHEN p.status = 1 THEN NOW() ELSE NULL END,
                    p.created_at,
                    p.updated_at,
                    p.status,
                    1,  -- ContentType.Page
                    p.featured_image_id
                FROM pages p;
            ");

            // Step 4: Create article_contents version 1 for each migrated page
            migrationBuilder.Sql(@"
                INSERT INTO article_contents (article_id, content, excerpt, meta_title, meta_description, version_number, created_at)
                SELECT
                    a.id,
                    p.content,
                    p.excerpt,
                    p.meta_title,
                    p.meta_description,
                    1,
                    p.created_at
                FROM pages p
                JOIN articles a ON (
                    a.content_type = 1
                    AND (a.slug = p.slug OR a.slug = p.slug || '-page')
                    AND a.created_at = p.created_at
                );
            ");

            // Step 5: Set published_version_id for published pages
            migrationBuilder.Sql(@"
                UPDATE articles a
                SET published_version_id = ac.id
                FROM article_contents ac
                WHERE ac.article_id = a.id
                  AND ac.version_number = 1
                  AND a.content_type = 1
                  AND a.status = 1;
            ");

            // Step 6a: Drop old FK constraint first so we can update page_id to article IDs
            migrationBuilder.DropForeignKey(
                name: "f_k_navigation_items__pages_page_id",
                table: "navigation_items");

            // Step 6b: Update navigation_items to reference new article IDs
            migrationBuilder.Sql(@"
                UPDATE navigation_items ni
                SET page_id = a.id
                FROM pages p
                JOIN articles a ON (
                    a.content_type = 1
                    AND (a.slug = p.slug OR a.slug = p.slug || '-page')
                    AND a.created_at = p.created_at
                )
                WHERE ni.page_id = p.id;
            ");

            // Step 7: Rename page_id to article_id in navigation_items
            migrationBuilder.RenameColumn(
                name: "page_id",
                table: "navigation_items",
                newName: "article_id");

            migrationBuilder.RenameIndex(
                name: "i_x_navigation_items_page_id",
                table: "navigation_items",
                newName: "i_x_navigation_items_article_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_navigation_items_articles_article_id",
                table: "navigation_items",
                column: "article_id",
                principalTable: "articles",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Step 8: Drop pages table
            migrationBuilder.DropTable(
                name: "pages");

            // Step 9: Update seed data - insert new Article-type page seeds
            // (only if they don't already exist from the data migration above)
            migrationBuilder.Sql(@"
                INSERT INTO articles (id, title, slug, status, content_type, created_at)
                SELECT 1000, 'Hjem', 'hjem', 1, 1, '2024-01-01T00:00:00Z'
                WHERE NOT EXISTS (SELECT 1 FROM articles WHERE slug = 'hjem' AND content_type = 1);

                INSERT INTO articles (id, title, slug, status, content_type, created_at)
                SELECT 1001, 'Om os', 'om', 1, 1, '2024-01-01T00:00:00Z'
                WHERE NOT EXISTS (SELECT 1 FROM articles WHERE slug = 'om' AND content_type = 1);

                INSERT INTO article_contents (id, article_id, content, excerpt, meta_title, meta_description, version_number, created_at)
                SELECT 1000, a.id,
                    '# Velkommen\n\nUdforsk vores seneste artikler, vejledninger og indsigter. Hold dig opdateret med det bedste indhold, kurateret specielt til dig.',
                    'Velkommen til vores hjemmeside',
                    'Forside',
                    'Udforsk vores seneste artikler og indsigter.',
                    1, '2024-01-01T00:00:00Z'
                FROM articles a WHERE a.slug = 'hjem' AND a.content_type = 1
                AND NOT EXISTS (SELECT 1 FROM article_contents WHERE article_id = a.id);

                INSERT INTO article_contents (id, article_id, content, excerpt, meta_title, meta_description, version_number, created_at)
                SELECT 1001, a.id,
                    '# Om os\n\nVelkommen til vores platform. Vores mission er at levere indhold af høj kvalitet, der informerer, uddanner og inspirerer.\n\nVi startede med et simpelt mål: at dele viden og indsigter med vores fællesskab. Over tid er vi vokset til en platform, der rummer diverst indhold på tværs af forskellige emner.\n\nVi hører gerne fra dig!',
                    'Lær mere om os og vores mission',
                    'Om os',
                    'Lær mere om vores platform og holdet bag.',
                    1, '2024-01-01T00:00:00Z'
                FROM articles a WHERE a.slug = 'om' AND a.content_type = 1
                AND NOT EXISTS (SELECT 1 FROM article_contents WHERE article_id = a.id);

                -- Update published_version_id for seed pages
                UPDATE articles SET published_version_id = (
                    SELECT id FROM article_contents WHERE article_id = articles.id AND version_number = 1 LIMIT 1
                )
                WHERE content_type = 1 AND status = 1 AND published_version_id IS NULL;

                -- Update navigation seed items to point to the om page article
                UPDATE navigation_items SET article_id = (
                    SELECT id FROM articles WHERE slug = 'om' AND content_type = 1 LIMIT 1
                )
                WHERE id IN (5, 8) AND article_id IS NULL;

                -- Reset sequences to avoid conflicts
                SELECT setval('articles_id_seq', GREATEST((SELECT MAX(id) FROM articles), 1001));
                SELECT setval('article_contents_id_seq', GREATEST((SELECT MAX(id) FROM article_contents), 1001));
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_articles__authors_author_id",
                table: "articles");

            migrationBuilder.DropForeignKey(
                name: "f_k_navigation_items_articles_article_id",
                table: "navigation_items");

            migrationBuilder.DropIndex(
                name: "IX_articles_content_type",
                table: "articles");

            // Remove migrated page article_contents and articles
            migrationBuilder.Sql(@"
                DELETE FROM article_contents WHERE article_id IN (SELECT id FROM articles WHERE content_type = 1);
                DELETE FROM articles WHERE content_type = 1;
            ");

            migrationBuilder.DropColumn(
                name: "content_type",
                table: "articles");

            migrationBuilder.RenameColumn(
                name: "article_id",
                table: "navigation_items",
                newName: "page_id");

            migrationBuilder.RenameIndex(
                name: "i_x_navigation_items_article_id",
                table: "navigation_items",
                newName: "i_x_navigation_items_page_id");

            migrationBuilder.AlterColumn<int>(
                name: "author_id",
                table: "articles",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "pages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    featured_image_id = table.Column<int>(type: "integer", nullable: true),
                    content = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    excerpt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    meta_title = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pages", x => x.id);
                    table.ForeignKey(
                        name: "f_k_pages_media_featured_image_id",
                        column: x => x.featured_image_id,
                        principalTable: "media",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "pages",
                columns: new[] { "id", "content", "created_at", "excerpt", "featured_image_id", "meta_description", "meta_title", "slug", "status", "title", "updated_at" },
                values: new object[,]
                {
                    { 1, "# Velkommen\n\nUdforsk vores seneste artikler, vejledninger og indsigter. Hold dig opdateret med det bedste indhold, kurateret specielt til dig.", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Velkommen til vores hjemmeside", null, "Udforsk vores seneste artikler og indsigter.", "Forside", "hjem", 1, "Hjem", null },
                    { 2, "# Om os\n\nVelkommen til vores platform. Vores mission er at levere indhold af høj kvalitet, der informerer, uddanner og inspirerer.\n\n## Vores historie\n\nVi startede med et simpelt mål: at dele viden og indsigter med vores fællesskab. Over tid er vi vokset til en platform, der rummer diverst indhold på tværs af forskellige emner.\n\n## Kontakt\n\nVi hører gerne fra dig! Uanset om du har feedback, forslag eller bare vil sige hej, er du velkommen til at kontakte os.", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lær mere om os og vores mission", null, "Lær mere om vores platform og holdet bag.", "Om os", "om", 1, "Om os", null }
                });

            migrationBuilder.UpdateData(
                table: "navigation_items",
                keyColumn: "id",
                keyValue: 5,
                column: "page_id",
                value: 2);

            migrationBuilder.UpdateData(
                table: "navigation_items",
                keyColumn: "id",
                keyValue: 8,
                column: "page_id",
                value: 2);

            migrationBuilder.CreateIndex(
                name: "i_x_pages_featured_image_id",
                table: "pages",
                column: "featured_image_id");

            migrationBuilder.CreateIndex(
                name: "IX_pages_slug",
                table: "pages",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "f_k_articles__authors_author_id",
                table: "articles",
                column: "author_id",
                principalTable: "authors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "f_k_navigation_items__pages_page_id",
                table: "navigation_items",
                column: "page_id",
                principalTable: "pages",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
