using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFinder.Migrations
{
    /// <inheritdoc />
    public partial class DiscordServersFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Channel_Groups_OwningGroupId",
                table: "Channel"
            );

            migrationBuilder.DropForeignKey(name: "FK_Channel_Server_ServerId", table: "Channel");

            migrationBuilder.DropForeignKey(name: "FK_Courses_Server_ServerId", table: "Courses");

            migrationBuilder.DropPrimaryKey(name: "PK_Server", table: "Server");

            migrationBuilder.DropPrimaryKey(name: "PK_Channel", table: "Channel");

            migrationBuilder.RenameTable(name: "Server", newName: "DiscordServers");

            migrationBuilder.RenameTable(name: "Channel", newName: "DiscordChannels");

            migrationBuilder.RenameIndex(
                name: "IX_Channel_ServerId",
                table: "DiscordChannels",
                newName: "IX_DiscordChannels_ServerId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_Channel_OwningGroupId",
                table: "DiscordChannels",
                newName: "IX_DiscordChannels_OwningGroupId"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_DiscordServers",
                table: "DiscordServers",
                column: "Id"
            );

            migrationBuilder.AddPrimaryKey(
                name: "PK_DiscordChannels",
                table: "DiscordChannels",
                column: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_DiscordServers_ServerId",
                table: "Courses",
                column: "ServerId",
                principalTable: "DiscordServers",
                principalColumn: "Id"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordChannels_DiscordServers_ServerId",
                table: "DiscordChannels",
                column: "ServerId",
                principalTable: "DiscordServers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_DiscordChannels_Groups_OwningGroupId",
                table: "DiscordChannels",
                column: "OwningGroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_DiscordServers_ServerId",
                table: "Courses"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_DiscordChannels_DiscordServers_ServerId",
                table: "DiscordChannels"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_DiscordChannels_Groups_OwningGroupId",
                table: "DiscordChannels"
            );

            migrationBuilder.DropPrimaryKey(name: "PK_DiscordServers", table: "DiscordServers");

            migrationBuilder.DropPrimaryKey(name: "PK_DiscordChannels", table: "DiscordChannels");

            migrationBuilder.RenameTable(name: "DiscordServers", newName: "Server");

            migrationBuilder.RenameTable(name: "DiscordChannels", newName: "Channel");

            migrationBuilder.RenameIndex(
                name: "IX_DiscordChannels_ServerId",
                table: "Channel",
                newName: "IX_Channel_ServerId"
            );

            migrationBuilder.RenameIndex(
                name: "IX_DiscordChannels_OwningGroupId",
                table: "Channel",
                newName: "IX_Channel_OwningGroupId"
            );

            migrationBuilder.AddPrimaryKey(name: "PK_Server", table: "Server", column: "Id");

            migrationBuilder.AddPrimaryKey(name: "PK_Channel", table: "Channel", column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_Groups_OwningGroupId",
                table: "Channel",
                column: "OwningGroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Channel_Server_ServerId",
                table: "Channel",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Server_ServerId",
                table: "Courses",
                column: "ServerId",
                principalTable: "Server",
                principalColumn: "Id"
            );
        }
    }
}
