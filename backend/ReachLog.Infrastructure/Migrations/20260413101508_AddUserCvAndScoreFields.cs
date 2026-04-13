using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReachLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserCvAndScoreFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MatchScore",
                table: "Outreaches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissingSkills",
                table: "Outreaches",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserCvs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtractedText = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserCvs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserCvs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserCvs_UserId",
                table: "UserCvs",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserCvs");

            migrationBuilder.DropColumn(
                name: "MatchScore",
                table: "Outreaches");

            migrationBuilder.DropColumn(
                name: "MissingSkills",
                table: "Outreaches");
        }
    }
}
