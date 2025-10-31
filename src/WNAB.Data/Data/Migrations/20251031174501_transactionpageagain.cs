using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WNAB.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class transactionpageagain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "TransactionSplits",
                newName: "Description");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Description",
                table: "TransactionSplits",
                newName: "Notes");
        }
    }
}
