using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRemember.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoApproveToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoApprovePhotos",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoApprovePhotos",
                table: "Events");
        }
    }
}
