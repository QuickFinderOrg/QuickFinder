using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace group_finder.Data.Migrations
{
    /// <inheritdoc />
    public partial class Waiting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WaitingPeople",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaitingPeople", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WaitingPeople_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WaitingPeople_PersonId",
                table: "WaitingPeople",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WaitingPeople");
        }
    }
}
