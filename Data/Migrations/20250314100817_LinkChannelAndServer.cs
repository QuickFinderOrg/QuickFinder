using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFinder.Migrations
{
    /// <inheritdoc />
    public partial class LinkChannelAndServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "ServerId",
                table: "Courses",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "CategoryId",
                table: "Channel",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.CreateTable(
                name: "Server",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CategoryId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Server", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_ServerId",
                table: "Courses",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Channel_ServerId",
                table: "Channel",
                column: "ServerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_Server_ServerId",
                table: "Channel",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Server_ServerId",
                table: "Courses",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channel_Server_ServerId",
                table: "Channel");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Server_ServerId",
                table: "Courses");

            migrationBuilder.DropTable(
                name: "Server");

            migrationBuilder.DropIndex(
                name: "IX_Courses_ServerId",
                table: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Channel_ServerId",
                table: "Channel");

            migrationBuilder.DropColumn(
                name: "ServerId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Channel");
        }
    }
}
