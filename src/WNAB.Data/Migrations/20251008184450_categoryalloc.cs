using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WNAB.Data.Migrations
{
    /// <inheritdoc />
    public partial class categoryalloc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionSplits_Categories_CategoryId",
                table: "TransactionSplits");

            migrationBuilder.RenameColumn(
                name: "CategoryId",
                table: "TransactionSplits",
                newName: "CategoryAllocationId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionSplits_CategoryId",
                table: "TransactionSplits",
                newName: "IX_TransactionSplits_CategoryAllocationId");

            migrationBuilder.AddColumn<bool>(
                name: "IsIncome",
                table: "TransactionSplits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EditedMemo",
                table: "Allocations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditorName",
                table: "Allocations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Allocations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "OldAmount",
                table: "Allocations",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentageAllocation",
                table: "Allocations",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionSplits_Allocations_CategoryAllocationId",
                table: "TransactionSplits",
                column: "CategoryAllocationId",
                principalTable: "Allocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionSplits_Allocations_CategoryAllocationId",
                table: "TransactionSplits");

            migrationBuilder.DropColumn(
                name: "IsIncome",
                table: "TransactionSplits");

            migrationBuilder.DropColumn(
                name: "EditedMemo",
                table: "Allocations");

            migrationBuilder.DropColumn(
                name: "EditorName",
                table: "Allocations");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Allocations");

            migrationBuilder.DropColumn(
                name: "OldAmount",
                table: "Allocations");

            migrationBuilder.DropColumn(
                name: "PercentageAllocation",
                table: "Allocations");

            migrationBuilder.RenameColumn(
                name: "CategoryAllocationId",
                table: "TransactionSplits",
                newName: "CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionSplits_CategoryAllocationId",
                table: "TransactionSplits",
                newName: "IX_TransactionSplits_CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionSplits_Categories_CategoryId",
                table: "TransactionSplits",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
