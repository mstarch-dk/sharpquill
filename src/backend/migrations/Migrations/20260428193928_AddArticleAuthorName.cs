using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddArticleAuthorName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "author_name",
                table: "articles",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "articles",
                keyColumn: "id",
                keyValue: 1000,
                column: "author_name",
                value: null);

            migrationBuilder.UpdateData(
                table: "articles",
                keyColumn: "id",
                keyValue: 1001,
                column: "author_name",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "author_name",
                table: "articles");
        }
    }
}
