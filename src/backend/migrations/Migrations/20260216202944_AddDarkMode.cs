using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddDarkMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO site_settings (key, value) VALUES ('dark_mode_admin', 'false') ON CONFLICT (key) DO NOTHING;");
            migrationBuilder.Sql(
                "INSERT INTO site_settings (key, value) VALUES ('dark_mode_web', 'false') ON CONFLICT (key) DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM site_settings WHERE key IN ('dark_mode_admin', 'dark_mode_web');");
        }
    }
}
