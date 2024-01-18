using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenTournament.Migrations
{
    /// <inheritdoc />
    public partial class ColumnTypes2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Tournaments",
                type: "varchar(36)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Participants",
                type: "varchar(36)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Tournaments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "varchar(36)");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "Participants",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "varchar(36)");
        }
    }
}
