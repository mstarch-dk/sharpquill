using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddFeaturedImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "featured_image_id",
                table: "pages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "featured_image_id",
                table: "articles",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "pages",
                keyColumn: "id",
                keyValue: 1,
                column: "featured_image_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "pages",
                keyColumn: "id",
                keyValue: 2,
                column: "featured_image_id",
                value: null);

            migrationBuilder.CreateIndex(
                name: "i_x_pages_featured_image_id",
                table: "pages",
                column: "featured_image_id");

            migrationBuilder.CreateIndex(
                name: "i_x_articles_featured_image_id",
                table: "articles",
                column: "featured_image_id");

            migrationBuilder.AddForeignKey(
                name: "f_k_articles__media_featured_image_id",
                table: "articles",
                column: "featured_image_id",
                principalTable: "media",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "f_k_pages_media_featured_image_id",
                table: "pages",
                column: "featured_image_id",
                principalTable: "media",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "f_k_articles__media_featured_image_id",
                table: "articles");

            migrationBuilder.DropForeignKey(
                name: "f_k_pages_media_featured_image_id",
                table: "pages");

            migrationBuilder.DropIndex(
                name: "i_x_pages_featured_image_id",
                table: "pages");

            migrationBuilder.DropIndex(
                name: "i_x_articles_featured_image_id",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "featured_image_id",
                table: "pages");

            migrationBuilder.DropColumn(
                name: "featured_image_id",
                table: "articles");
        }
    }
}
