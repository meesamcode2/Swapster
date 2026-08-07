using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Swapster.Migrations
{
    /// <inheritdoc />
    public partial class settings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeactivated",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    UserSettingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PublicProfile = table.Column<bool>(type: "bit", nullable: false),
                    ShowMySkills = table.Column<bool>(type: "bit", nullable: false),
                    EmailNotifications = table.Column<bool>(type: "bit", nullable: false),
                    SkillRequestAlerts = table.Column<bool>(type: "bit", nullable: false),
                    RecommendationUpdates = table.Column<bool>(type: "bit", nullable: false),
                    ShowEmailToOthers = table.Column<bool>(type: "bit", nullable: false),
                    AllowMatchSuggestions = table.Column<bool>(type: "bit", nullable: false),
                    ProfileSearchVisibility = table.Column<bool>(type: "bit", nullable: false),
                    ThemeMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    BrandColor = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CompactDashboard = table.Column<bool>(type: "bit", nullable: false),
                    EnableAiAssistant = table.Column<bool>(type: "bit", nullable: false),
                    PersonalizedRecommendations = table.Column<bool>(type: "bit", nullable: false),
                    CareerGuidance = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.UserSettingId);
                    table.ForeignKey(
                        name: "FK_UserSettings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "IsDeactivated",
                table: "Users");
        }
    }
}
