using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaitingForTheSummer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsAndBonusQuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBonus",
                table: "Quests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "BonusForRegularPairEnd",
                table: "GameRounds",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EligibleTeam",
                table: "GameRounds",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "GameRounds",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "AspNetUsers",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_GameRounds_BonusForRegularPairEnd",
                table: "GameRounds",
                column: "BonusForRegularPairEnd");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_GameRounds_BonusForRegularPairEnd",
                table: "GameRounds");

            migrationBuilder.DropColumn(
                name: "IsBonus",
                table: "Quests");

            migrationBuilder.DropColumn(
                name: "BonusForRegularPairEnd",
                table: "GameRounds");

            migrationBuilder.DropColumn(
                name: "EligibleTeam",
                table: "GameRounds");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "GameRounds");

            migrationBuilder.DropColumn(
                name: "Gender",
                table: "AspNetUsers");
        }
    }
}
