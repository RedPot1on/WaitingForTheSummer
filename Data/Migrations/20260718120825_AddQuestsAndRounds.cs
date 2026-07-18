using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaitingForTheSummer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestsAndRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Quests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    BodyContent = table.Column<string>(type: "TEXT", nullable: false),
                    IconPath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsPublished = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOnceOnly = table.Column<bool>(type: "INTEGER", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    QuestId = table.Column<int>(type: "INTEGER", nullable: false),
                    RequiredQuestId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuestRequirements_Quests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuestRequirements_Quests_RequiredQuestId",
                        column: x => x.RequiredQuestId,
                        principalTable: "Quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rounds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    QuestId = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedByAdminId = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rounds_AspNetUsers_ResolvedByAdminId",
                        column: x => x.ResolvedByAdminId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Rounds_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rounds_Quests_QuestId",
                        column: x => x.QuestId,
                        principalTable: "Quests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuestRequirements_QuestId_RequiredQuestId",
                table: "QuestRequirements",
                columns: new[] { "QuestId", "RequiredQuestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuestRequirements_RequiredQuestId",
                table: "QuestRequirements",
                column: "RequiredQuestId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_QuestId",
                table: "Rounds",
                column: "QuestId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_ResolvedByAdminId",
                table: "Rounds",
                column: "ResolvedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Rounds_UserId_Status",
                table: "Rounds",
                columns: new[] { "UserId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuestRequirements");

            migrationBuilder.DropTable(
                name: "Rounds");

            migrationBuilder.DropTable(
                name: "Quests");
        }
    }
}
