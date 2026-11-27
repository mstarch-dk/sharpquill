using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace SharpQuill.Migrations.Migrations
{
    [DbContext(typeof(SharpQuillDbContext))]
    [Migration("20260307212000_AddFeaturedImageSizeToArticleContents")]
    public partial class AddFeaturedImageSizeToArticleContents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "featured_image_size",
                table: "article_contents",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "featured_image_size",
                table: "article_contents");
        }
    }
}
