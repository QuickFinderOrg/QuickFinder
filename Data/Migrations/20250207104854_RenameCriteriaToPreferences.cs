using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFinder.Migrations
{
    /// <inheritdoc />
    public partial class RenameCriteriaToPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Criteria_Availability",
                table: "Groups",
                newName: "Preferences_Availability");

            migrationBuilder.RenameColumn(
                name: "Criteria_Availability",
                table: "AspNetUsers",
                newName: "Preferences_Availability");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Preferences_Availability",
                table: "Groups",
                newName: "Criteria_Availability");

            migrationBuilder.RenameColumn(
                name: "Preferences_Availability",
                table: "AspNetUsers",
                newName: "Criteria_Availability");
        }
    }
}
