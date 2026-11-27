using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPageStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "page_style",
                table: "article_contents",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "article_contents",
                keyColumn: "id",
                keyValue: 1000,
                column: "page_style",
                value: null);

            migrationBuilder.UpdateData(
                table: "article_contents",
                keyColumn: "id",
                keyValue: 1001,
                column: "page_style",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "page_style",
                table: "article_contents");
        }
    }
}
