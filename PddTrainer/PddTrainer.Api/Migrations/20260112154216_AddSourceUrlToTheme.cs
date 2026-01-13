using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PddTrainer.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceUrlToTheme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceUrl",
                table: "Themes",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceUrl",
                table: "Themes");
        }
    }
}
