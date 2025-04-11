using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFinder.Migrations
{
    /// <inheritdoc />
    public partial class GlobalUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GlobalAvailability",
                table: "Preferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "GlobalDays",
                table: "Preferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "Preferences_GlobalAvailability",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<int>(
                name: "Preferences_GlobalDays",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "GlobalAvailability", table: "Preferences");

            migrationBuilder.DropColumn(name: "GlobalDays", table: "Preferences");

            migrationBuilder.DropColumn(
                name: "Preferences_GlobalAvailability",
                table: "AspNetUsers"
            );

            migrationBuilder.DropColumn(name: "Preferences_GlobalDays", table: "AspNetUsers");
        }
    }
}
