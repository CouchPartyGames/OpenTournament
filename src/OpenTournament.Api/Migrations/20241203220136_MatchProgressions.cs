using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class MatchProgressions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "Tournaments",
                newName: "StartedOnUtc");

            migrationBuilder.RenameColumn(
                name: "Completed",
                table: "Tournaments",
                newName: "CompletedOnUtc");

            migrationBuilder.AddColumn<int>(
                name: "LoseProgressionId",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WinProgressionId",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoseProgressionId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "WinProgressionId",
                table: "Matches");

            migrationBuilder.RenameColumn(
                name: "StartedOnUtc",
                table: "Tournaments",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "CompletedOnUtc",
                table: "Tournaments",
                newName: "Completed");
        }
    }
}
