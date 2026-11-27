using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPagesAndNavigations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "navigations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navigations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pages",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: true),
                    excerpt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    meta_title = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    meta_description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "navigation_items",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    navigation_id = table.Column<int>(type: "integer", nullable: false),
                    parent_id = table.Column<int>(type: "integer", nullable: true),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    item_type = table.Column<int>(type: "integer", nullable: false),
                    page_id = table.Column<int>(type: "integer", nullable: true),
                    system_route = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    position = table.Column<int>(type: "integer", nullable: false),
                    open_in_new_tab = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_navigation_items", x => x.id);
                    table.ForeignKey(
                        name: "f_k_navigation_items__pages_page_id",
                        column: x => x.page_id,
                        principalTable: "pages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "f_k_navigation_items_navigation_items_parent_id",
                        column: x => x.parent_id,
                        principalTable: "navigation_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "f_k_navigation_items_navigations_navigation_id",
                        column: x => x.navigation_id,
                        principalTable: "navigations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "navigations",
                columns: new[] { "id", "name", "slug" },
                values: new object[,]
                {
                    { 1, "Hovedmenu", "main-menu" },
                    { 2, "Footer-links", "footer-links" }
                });

            migrationBuilder.InsertData(
                table: "pages",
                columns: new[] { "id", "content", "created_at", "excerpt", "meta_description", "meta_title", "slug", "status", "title", "updated_at" },
                values: new object[,]
                {
                    { 1, "# Velkommen\n\nUdforsk vores seneste artikler, vejledninger og indsigter. Hold dig opdateret med det bedste indhold, kurateret specielt til dig.", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Velkommen til vores hjemmeside", "Udforsk vores seneste artikler og indsigter.", "Forside", "hjem", 1, "Hjem", null },
                    { 2, "# Om os\n\nVelkommen til vores platform. Vores mission er at levere indhold af høj kvalitet, der informerer, uddanner og inspirerer.\n\n## Vores historie\n\nVi startede med et simpelt mål: at dele viden og indsigter med vores fællesskab. Over tid er vi vokset til en platform, der rummer diverst indhold på tværs af forskellige emner.\n\n## Kontakt\n\nVi hører gerne fra dig! Uanset om du har feedback, forslag eller bare vil sige hej, er du velkommen til at kontakte os.", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Lær mere om os og vores mission", "Lær mere om vores platform og holdet bag.", "Om os", "om", 1, "Om os", null }
                });

            migrationBuilder.InsertData(
                table: "navigation_items",
                columns: new[] { "id", "item_type", "label", "navigation_id", "open_in_new_tab", "page_id", "parent_id", "position", "system_route", "url" },
                values: new object[,]
                {
                    { 1, 1, "Hjem", 1, false, null, null, 1, "/", null },
                    { 2, 1, "Blog", 1, false, null, null, 2, "/blog", null },
                    { 3, 1, "Kategorier", 1, false, null, null, 3, "/categories", null },
                    { 4, 1, "Serier", 1, false, null, null, 4, "/series", null },
                    { 5, 0, "Om", 1, false, 2, null, 5, null, null },
                    { 6, 1, "Hjem", 2, false, null, null, 1, "/", null },
                    { 7, 1, "Blog", 2, false, null, null, 2, "/blog", null },
                    { 8, 0, "Om", 2, false, 2, null, 3, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "i_x_navigation_items_navigation_id",
                table: "navigation_items",
                column: "navigation_id");

            migrationBuilder.CreateIndex(
                name: "i_x_navigation_items_page_id",
                table: "navigation_items",
                column: "page_id");

            migrationBuilder.CreateIndex(
                name: "i_x_navigation_items_parent_id",
                table: "navigation_items",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_navigations_slug",
                table: "navigations",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_pages_slug",
                table: "pages",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "navigation_items");

            migrationBuilder.DropTable(
                name: "pages");

            migrationBuilder.DropTable(
                name: "navigations");
        }
    }
}
