using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFinder.Migrations
{
    /// <inheritdoc />
    public partial class AddServerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Server",
                type: "TEXT",
                nullable: false,
                defaultValue: ""
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Name", table: "Server");
        }
    }
}
