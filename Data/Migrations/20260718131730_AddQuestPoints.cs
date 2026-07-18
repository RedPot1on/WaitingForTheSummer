using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaitingForTheSummer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PointsAwarded",
                table: "Rounds",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Points",
                table: "Quests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PointsAwarded",
                table: "Rounds");

            migrationBuilder.DropColumn(
                name: "Points",
                table: "Quests");
        }
    }
}
