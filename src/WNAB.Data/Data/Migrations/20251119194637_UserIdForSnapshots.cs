using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WNAB.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class UserIdForSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "BudgetSnapshots",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_BudgetSnapshots_UserId",
                table: "BudgetSnapshots",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_BudgetSnapshots_Users_UserId",
                table: "BudgetSnapshots",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BudgetSnapshots_Users_UserId",
                table: "BudgetSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_BudgetSnapshots_UserId",
                table: "BudgetSnapshots");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "BudgetSnapshots");
        }
    }
}
