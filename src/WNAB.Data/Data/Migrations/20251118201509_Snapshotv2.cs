using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WNAB.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class Snapshotv2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RTA",
                table: "BudgetSnapshots",
                newName: "SnapshotReadyToAssign");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SnapshotReadyToAssign",
                table: "BudgetSnapshots",
                newName: "RTA");
        }
    }
}
