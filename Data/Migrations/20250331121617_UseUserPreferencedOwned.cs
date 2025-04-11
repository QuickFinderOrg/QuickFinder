using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFinder.Migrations
{
    /// <inheritdoc />
    public partial class UseUserPreferencedOwned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Preferences_Availability", table: "Groups");

            migrationBuilder.DropColumn(name: "Preferences_GroupSize", table: "Groups");

            migrationBuilder.DropColumn(name: "Preferences_Availability", table: "AspNetUsers");

            migrationBuilder.DropColumn(name: "Preferences_GroupSize", table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "Preferences_Language",
                table: "Groups",
                newName: "PreferencesId"
            );

            migrationBuilder.AddColumn<Guid>(
                name: "PreferencesId",
                table: "Tickets",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
            );

            migrationBuilder.CreateTable(
                name: "CoursePreferences",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    CourseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Availability = table.Column<int>(type: "INTEGER", nullable: false),
                    GroupSize = table.Column<uint>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CoursePreferences", x => new { x.UserId, x.CourseId });
                    table.ForeignKey(
                        name: "FK_CoursePreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                    table.ForeignKey(
                        name: "FK_CoursePreferences_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Preferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    Availability = table.Column<int>(type: "INTEGER", nullable: false),
                    GroupSize = table.Column<uint>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Preferences", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PreferencesId",
                table: "Tickets",
                column: "PreferencesId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Groups_PreferencesId",
                table: "Groups",
                column: "PreferencesId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_CoursePreferences_CourseId",
                table: "CoursePreferences",
                column: "CourseId"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Groups_Preferences_PreferencesId",
                table: "Groups",
                column: "PreferencesId",
                principalTable: "Preferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Preferences_PreferencesId",
                table: "Tickets",
                column: "PreferencesId",
                principalTable: "Preferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Groups_Preferences_PreferencesId",
                table: "Groups"
            );

            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_Preferences_PreferencesId",
                table: "Tickets"
            );

            migrationBuilder.DropTable(name: "CoursePreferences");

            migrationBuilder.DropTable(name: "Preferences");

            migrationBuilder.DropIndex(name: "IX_Tickets_PreferencesId", table: "Tickets");

            migrationBuilder.DropIndex(name: "IX_Groups_PreferencesId", table: "Groups");

            migrationBuilder.DropColumn(name: "PreferencesId", table: "Tickets");

            migrationBuilder.RenameColumn(
                name: "PreferencesId",
                table: "Groups",
                newName: "Preferences_Language"
            );

            migrationBuilder.AddColumn<int>(
                name: "Preferences_Availability",
                table: "Groups",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<uint>(
                name: "Preferences_GroupSize",
                table: "Groups",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u
            );

            migrationBuilder.AddColumn<int>(
                name: "Preferences_Availability",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0
            );

            migrationBuilder.AddColumn<uint>(
                name: "Preferences_GroupSize",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0u
            );
        }
    }
}
