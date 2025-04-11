using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFinder.Migrations
{
    /// <inheritdoc />
    public partial class DiscordChannelOwningGroupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DiscordChannels_Groups_OwningGroupId",
                table: "DiscordChannels"
            );

            migrationBuilder.DropIndex(
                name: "IX_DiscordChannels_OwningGroupId",
                table: "DiscordChannels"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_DiscordChannels_OwningGroupId",
                table: "DiscordChannels",
                column: "OwningGroupId"
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
    }
}
