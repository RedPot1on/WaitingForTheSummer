using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaitingForTheSummer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSideGameRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Points",
                table: "SideGameScores",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "SideGameScores",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                table: "SideGameScores");

            migrationBuilder.AlterColumn<decimal>(
                name: "Points",
                table: "SideGameScores",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
