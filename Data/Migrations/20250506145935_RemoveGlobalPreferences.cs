using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFinder.Migrations
{
    /// <inheritdoc />
    public partial class RemoveGlobalPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GlobalAvailability",
                table: "Preferences");

            migrationBuilder.DropColumn(
                name: "GlobalDays",
                table: "Preferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GlobalAvailability",
                table: "Preferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GlobalDays",
                table: "Preferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
