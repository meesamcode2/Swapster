using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swapster.Migrations
{
    /// <inheritdoc />
    public partial class tokkens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiry",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenExpiry",
                table: "Users");
        }
    }
}
