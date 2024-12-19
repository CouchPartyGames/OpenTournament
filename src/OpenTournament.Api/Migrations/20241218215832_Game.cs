using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenTournament.Api.Migrations
{
    /// <inheritdoc />
    public partial class Game : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Game",
                columns: table => new
                {
                    GameId = table.Column<string>(type: "varchar(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(36)", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Game", x => x.GameId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Game");
        }
    }
}
