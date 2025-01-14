using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace group_finder.Data.Migrations
{
    /// <inheritdoc />
    public partial class CriteriaInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Preferences_Availability",
                table: "People",
                newName: "Criteria_Availability");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Criteria_Availability",
                table: "People",
                newName: "Preferences_Availability");
        }
    }
}
