using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "i_x_refresh_tokens_token",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_token");

            migrationBuilder.AddColumn<int>(
                name: "series_id",
                table: "articles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "series_order",
                table: "articles",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "series",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_series", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "i_x_articles_series_id",
                table: "articles",
                column: "series_id");

            migrationBuilder.CreateIndex(
                name: "IX_series_slug",
                table: "series",
                column: "slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "f_k_articles__series_series_id",
                table: "articles",
                column: "series_id",
                principalTable: "series",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_articles__series_series_id",
                table: "articles");

            migrationBuilder.DropTable(
                name: "series");

            migrationBuilder.DropIndex(
                name: "i_x_articles_series_id",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "series_id",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "series_order",
                table: "articles");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_token",
                table: "refresh_tokens",
                newName: "i_x_refresh_tokens_token");
        }
    }
}
