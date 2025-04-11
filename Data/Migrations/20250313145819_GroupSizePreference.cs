using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFinder.Migrations
{
    /// <inheritdoc />
    public partial class GroupSizePreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "Preferences_GroupSize",
                table: "Groups",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u
            );

            migrationBuilder.AddColumn<uint>(
                name: "Preferences_GroupSize",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Preferences_GroupSize", table: "Groups");

            migrationBuilder.DropColumn(name: "Preferences_GroupSize", table: "AspNetUsers");
        }
    }
}
