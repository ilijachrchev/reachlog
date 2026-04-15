using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReachLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SharedJobsArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScrapedJobs_Users_UserId",
                table: "ScrapedJobs");

            migrationBuilder.DropIndex(
                name: "IX_ScrapedJobs_UserId",
                table: "ScrapedJobs");

            migrationBuilder.DropColumn(
                name: "ImportedOutreachId",
                table: "ScrapedJobs");

            migrationBuilder.DropColumn(
                name: "IsImported",
                table: "ScrapedJobs");

            migrationBuilder.DropColumn(
                name: "MatchScore",
                table: "ScrapedJobs");

            migrationBuilder.DropColumn(
                name: "MissingSkills",
                table: "ScrapedJobs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ScrapedJobs");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ScrapeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsHandled = table.Column<bool>(type: "boolean", nullable: false),
                    HandledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScrapeRequests_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScraperSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LastScrapedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalJobsInFeed = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScraperSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserJobInteractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScrapedJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchScore = table.Column<int>(type: "integer", nullable: true),
                    MissingSkills = table.Column<string>(type: "text", nullable: true),
                    IsImported = table.Column<bool>(type: "boolean", nullable: false),
                    ImportedOutreachId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserJobInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserJobInteractions_ScrapedJobs_ScrapedJobId",
                        column: x => x.ScrapedJobId,
                        principalTable: "ScrapedJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserJobInteractions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserJobPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    JobType = table.Column<string>(type: "text", nullable: false),
                    Keywords = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserJobPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserJobPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScrapeRequests_UserId",
                table: "ScrapeRequests",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserJobInteractions_ScrapedJobId",
                table: "UserJobInteractions",
                column: "ScrapedJobId");

            migrationBuilder.CreateIndex(
                name: "IX_UserJobInteractions_UserId",
                table: "UserJobInteractions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserJobPreferences_UserId",
                table: "UserJobPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScrapeRequests");

            migrationBuilder.DropTable(
                name: "ScraperSettings");

            migrationBuilder.DropTable(
                name: "UserJobInteractions");

            migrationBuilder.DropTable(
                name: "UserJobPreferences");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.AddColumn<Guid>(
                name: "ImportedOutreachId",
                table: "ScrapedJobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsImported",
                table: "ScrapedJobs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MatchScore",
                table: "ScrapedJobs",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissingSkills",
                table: "ScrapedJobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "ScrapedJobs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_ScrapedJobs_UserId",
                table: "ScrapedJobs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScrapedJobs_Users_UserId",
                table: "ScrapedJobs",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
