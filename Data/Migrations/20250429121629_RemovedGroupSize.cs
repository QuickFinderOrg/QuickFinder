using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFinder.Migrations
{
    /// <inheritdoc />
    public partial class RemovedGroupSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GroupSize",
                table: "Preferences");

            migrationBuilder.DropColumn(
                name: "AllowCustomSize",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "GroupSize",
                table: "CoursePreferences");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "GroupSize",
                table: "Preferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<bool>(
                name: "AllowCustomSize",
                table: "Courses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "GroupSize",
                table: "CoursePreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u);
        }
    }
}
