using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateThumbprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "certificate_thumbprint",
                table: "authors",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "authors",
                keyColumn: "id",
                keyValue: 1,
                column: "certificate_thumbprint",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_authors_certificate_thumbprint",
                table: "authors",
                column: "certificate_thumbprint",
                unique: true,
                filter: "certificate_thumbprint IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_authors_certificate_thumbprint",
                table: "authors");

            migrationBuilder.DropColumn(
                name: "certificate_thumbprint",
                table: "authors");
        }
    }
}
