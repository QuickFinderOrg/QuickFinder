﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuickFinder.Migrations
{
    /// <inheritdoc />
    public partial class RequireCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Tickets_Courses_CourseId", table: "Tickets");

            migrationBuilder.AlterColumn<Guid>(
                name: "CourseId",
                table: "Tickets",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Courses_CourseId",
                table: "Tickets",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Tickets_Courses_CourseId", table: "Tickets");

            migrationBuilder.AlterColumn<Guid>(
                name: "CourseId",
                table: "Tickets",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT"
            );

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_Courses_CourseId",
                table: "Tickets",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id"
            );
        }
    }
}
