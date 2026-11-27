using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "height",
                table: "media",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "medium_path",
                table: "media",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "thumbnail_path",
                table: "media",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "width",
                table: "media",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "height",
                table: "media");

            migrationBuilder.DropColumn(
                name: "medium_path",
                table: "media");

            migrationBuilder.DropColumn(
                name: "thumbnail_path",
                table: "media");

            migrationBuilder.DropColumn(
                name: "width",
                table: "media");
        }
    }
}
