using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRemember.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCaptionToPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Caption",
                table: "Photos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Caption",
                table: "Photos");
        }
    }
}
