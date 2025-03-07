using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace group_finder.Migrations
{
    /// <inheritdoc />
    public partial class AllowCustomSize : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowCustomSize",
                table: "Courses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowCustomSize",
                table: "Courses");
        }
    }
}
