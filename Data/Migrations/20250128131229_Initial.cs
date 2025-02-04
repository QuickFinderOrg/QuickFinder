using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace group_finder.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CourseId",
                table: "Tickets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CourseId",
                table: "Tickets",
                column: "CourseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Courses_CourseId",
                table: "Tickets",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Courses_CourseId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_CourseId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "CourseId",
                table: "Tickets");
        }
    }
}
