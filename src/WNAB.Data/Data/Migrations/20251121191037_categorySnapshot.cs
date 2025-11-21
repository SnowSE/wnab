using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WNAB.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class categorySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategorySnapshotData_BudgetSnapshots_BudgetSnapshotId",
                table: "CategorySnapshotData");

            migrationBuilder.DropForeignKey(
                name: "FK_CategorySnapshotData_Categories_CategoryId",
                table: "CategorySnapshotData");

            migrationBuilder.DropIndex(
                name: "IX_BudgetSnapshots_UserId",
                table: "BudgetSnapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CategorySnapshotData",
                table: "CategorySnapshotData");

            migrationBuilder.RenameTable(
                name: "CategorySnapshotData",
                newName: "CategorySnapshotDatas");

            migrationBuilder.RenameIndex(
                name: "IX_CategorySnapshotData_CategoryId",
                table: "CategorySnapshotDatas",
                newName: "IX_CategorySnapshotDatas_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_CategorySnapshotData_BudgetSnapshotId",
                table: "CategorySnapshotDatas",
                newName: "IX_CategorySnapshotDatas_BudgetSnapshotId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Available",
                table: "CategorySnapshotDatas",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "AssignedValue",
                table: "CategorySnapshotDatas",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "Activity",
                table: "CategorySnapshotDatas",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CategorySnapshotDatas",
                table: "CategorySnapshotDatas",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetSnapshots_UserId_Month_Year",
                table: "BudgetSnapshots",
                columns: new[] { "UserId", "Month", "Year" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CategorySnapshotDatas_BudgetSnapshots_BudgetSnapshotId",
                table: "CategorySnapshotDatas",
                column: "BudgetSnapshotId",
                principalTable: "BudgetSnapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CategorySnapshotDatas_Categories_CategoryId",
                table: "CategorySnapshotDatas",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CategorySnapshotDatas_BudgetSnapshots_BudgetSnapshotId",
                table: "CategorySnapshotDatas");

            migrationBuilder.DropForeignKey(
                name: "FK_CategorySnapshotDatas_Categories_CategoryId",
                table: "CategorySnapshotDatas");

            migrationBuilder.DropIndex(
                name: "IX_BudgetSnapshots_UserId_Month_Year",
                table: "BudgetSnapshots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CategorySnapshotDatas",
                table: "CategorySnapshotDatas");

            migrationBuilder.RenameTable(
                name: "CategorySnapshotDatas",
                newName: "CategorySnapshotData");

            migrationBuilder.RenameIndex(
                name: "IX_CategorySnapshotDatas_CategoryId",
                table: "CategorySnapshotData",
                newName: "IX_CategorySnapshotData_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_CategorySnapshotDatas_BudgetSnapshotId",
                table: "CategorySnapshotData",
                newName: "IX_CategorySnapshotData_BudgetSnapshotId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Available",
                table: "CategorySnapshotData",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AssignedValue",
                table: "CategorySnapshotData",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Activity",
                table: "CategorySnapshotData",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CategorySnapshotData",
                table: "CategorySnapshotData",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_BudgetSnapshots_UserId",
                table: "BudgetSnapshots",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CategorySnapshotData_BudgetSnapshots_BudgetSnapshotId",
                table: "CategorySnapshotData",
                column: "BudgetSnapshotId",
                principalTable: "BudgetSnapshots",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CategorySnapshotData_Categories_CategoryId",
                table: "CategorySnapshotData",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
