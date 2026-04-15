using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReachLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScrapedJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScrapedJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Company = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    IsRemote = table.Column<bool>(type: "boolean", nullable: false),
                    JobBoard = table.Column<string>(type: "text", nullable: false),
                    ExternalUrl = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    SalaryMin = table.Column<int>(type: "integer", nullable: true),
                    SalaryMax = table.Column<int>(type: "integer", nullable: true),
                    Currency = table.Column<string>(type: "text", nullable: true),
                    JobType = table.Column<string>(type: "text", nullable: true),
                    Wave = table.Column<int>(type: "integer", nullable: false),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ScrapedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsImported = table.Column<bool>(type: "boolean", nullable: false),
                    ImportedOutreachId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchScore = table.Column<int>(type: "integer", nullable: true),
                    MissingSkills = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapedJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScrapedJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScrapedJobs_UserId",
                table: "ScrapedJobs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScrapedJobs");
        }
    }
}
