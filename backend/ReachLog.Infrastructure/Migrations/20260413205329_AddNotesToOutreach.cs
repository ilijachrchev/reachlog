using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReachLog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotesToOutreach : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Outreaches",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Outreaches");
        }
    }
}
