using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WNAB.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsValidToBudgetSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsValid",
                table: "BudgetSnapshots",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsValid",
                table: "BudgetSnapshots");
        }
    }
}
