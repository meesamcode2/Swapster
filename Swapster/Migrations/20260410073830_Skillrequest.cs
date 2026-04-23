using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swapster.Migrations
{
    /// <inheritdoc />
    public partial class Skillrequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequestId",
                table: "SkillRequests",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SkillRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "SkillRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "SkillRequests");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "SkillRequests",
                newName: "RequestId");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "SkillRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
