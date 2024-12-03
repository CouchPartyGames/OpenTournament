using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class CompletionNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WinProgressionId",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: -1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "LoseProgressionId",
                table: "Matches",
                type: "integer",
                nullable: false,
                defaultValue: -1,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedOnUtc",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "null",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "WinProgressionId",
                table: "Matches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: -1);

            migrationBuilder.AlterColumn<int>(
                name: "LoseProgressionId",
                table: "Matches",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: -1);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CompletedOnUtc",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "null");
        }
    }
}
