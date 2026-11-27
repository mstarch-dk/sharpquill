using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "authors",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "admin");

            migrationBuilder.UpdateData(
                table: "authors",
                keyColumn: "id",
                keyValue: 1,
                column: "role",
                value: "admin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "role",
                table: "authors");
        }
    }
}
