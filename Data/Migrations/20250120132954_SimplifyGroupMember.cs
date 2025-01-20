using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace group_finder.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyGroupMember : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_People_Groups_GroupId",
                table: "People");

            migrationBuilder.DropIndex(
                name: "IX_People_GroupId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "People");

            migrationBuilder.AddColumn<string>(
                name: "Members",
                table: "Groups",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Members",
                table: "Groups");

            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "People",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_People_GroupId",
                table: "People",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_People_Groups_GroupId",
                table: "People",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id");
        }
    }
}
