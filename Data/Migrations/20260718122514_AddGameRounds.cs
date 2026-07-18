using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaitingForTheSummer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGameRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GameRoundId",
                table: "Rounds",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "GameRounds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedByAdminId = table.Column<string>(type: "TEXT", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosedByAdminId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameRounds_AspNetUsers_ClosedByAdminId",
                        column: x => x.ClosedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GameRounds_AspNetUsers_StartedByAdminId",
                        column: x => x.StartedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_GameRoundId_UserId",
                table: "Rounds",
                columns: new[] { "GameRoundId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameRounds_ClosedByAdminId",
                table: "GameRounds",
                column: "ClosedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_GameRounds_Number",
                table: "GameRounds",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameRounds_StartedByAdminId",
                table: "GameRounds",
                column: "StartedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_GameRounds_Status",
                table: "GameRounds",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Rounds_GameRounds_GameRoundId",
                table: "Rounds",
                column: "GameRoundId",
                principalTable: "GameRounds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Rounds_GameRounds_GameRoundId",
                table: "Rounds");

            migrationBuilder.DropTable(
                name: "GameRounds");

            migrationBuilder.DropIndex(
                name: "IX_Rounds_GameRoundId_UserId",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "GameRoundId",
                table: "Rounds");
        }
    }
}
