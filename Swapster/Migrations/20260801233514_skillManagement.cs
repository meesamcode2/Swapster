using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swapster.Migrations
{
    /// <inheritdoc />
    public partial class skillManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "UserWantedSkills",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserWantedSkills",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserWantedSkills",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "UserSkills",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserSkills",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UserSkills",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "UserWantedSkills");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserWantedSkills");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserWantedSkills");

            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "UserSkills");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserSkills");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UserSkills");
        }
    }
}
