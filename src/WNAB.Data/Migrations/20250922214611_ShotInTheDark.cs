using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WNAB.Data.Migrations
{
    /// <inheritdoc />
    public partial class ShotInTheDark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Payee",
                table: "Transactions",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Payee",
                table: "Transactions");
        }
    }
}
