using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class MatchCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Created",
                table: "Tournaments");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOnUtc",
                table: "Tournaments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CreatorId",
                table: "Tournaments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedOnUtc",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "WinnerId2",
                table: "Matches",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedOnUtc",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Tournaments");

            migrationBuilder.DropColumn(
                name: "CompletedOnUtc",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "WinnerId2",
                table: "Matches");

            migrationBuilder.AddColumn<DateTime>(
                name: "Created",
                table: "Tournaments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()");
        }
    }
}
