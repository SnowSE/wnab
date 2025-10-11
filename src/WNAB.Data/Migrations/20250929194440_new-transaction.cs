using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WNAB.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionSplit_Categories_CategoryId",
                table: "TransactionSplit");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionSplit_Transactions_TransactionId",
                table: "TransactionSplit");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransactionSplit",
                table: "TransactionSplit");

            migrationBuilder.RenameTable(
                name: "TransactionSplit",
                newName: "TransactionSplits");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionSplit_TransactionId",
                table: "TransactionSplits",
                newName: "IX_TransactionSplits_TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionSplit_CategoryId",
                table: "TransactionSplits",
                newName: "IX_TransactionSplits_CategoryId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "TransactionSplits",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TransactionSplits",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "TransactionSplits",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransactionSplits",
                table: "TransactionSplits",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionSplits_Categories_CategoryId",
                table: "TransactionSplits",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionSplits_Transactions_TransactionId",
                table: "TransactionSplits",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TransactionSplits_Categories_CategoryId",
                table: "TransactionSplits");

            migrationBuilder.DropForeignKey(
                name: "FK_TransactionSplits_Transactions_TransactionId",
                table: "TransactionSplits");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TransactionSplits",
                table: "TransactionSplits");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionSplits_TransactionId",
                table: "TransactionSplits",
                newName: "IX_TransactionSplit_TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_TransactionSplits_CategoryId",
                table: "TransactionSplits",
                newName: "IX_TransactionSplit_CategoryId");

            migrationBuilder.RenameTable(
                name: "TransactionSplits",
                newName: "TransactionSplit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "TransactionSplit",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TransactionSplit",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "TransactionSplit",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TransactionSplit",
                table: "TransactionSplit",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionSplit_Categories_CategoryId",
                table: "TransactionSplit",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TransactionSplit_Transactions_TransactionId",
                table: "TransactionSplit",
                column: "TransactionId",
                principalTable: "Transactions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
