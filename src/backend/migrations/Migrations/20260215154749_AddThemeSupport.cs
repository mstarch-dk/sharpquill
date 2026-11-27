using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddThemeSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "theme_slug",
                table: "series",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.InsertData(
                table: "site_settings",
                columns: new[] { "id", "key", "updated_at", "value" },
                values: new object[] { 1, "theme", null, "ocean-blue" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "site_settings",
                keyColumn: "id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "theme_slug",
                table: "series");
        }
    }
}
