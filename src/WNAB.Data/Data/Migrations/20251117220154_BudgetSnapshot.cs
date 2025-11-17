using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WNAB.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class BudgetSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BudgetSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    RTA = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BudgetSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategorySnapshotData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    AssignedValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Activity = table.Column<decimal>(type: "numeric", nullable: false),
                    Available = table.Column<decimal>(type: "numeric", nullable: false),
                    BudgetSnapshotId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategorySnapshotData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategorySnapshotData_BudgetSnapshots_BudgetSnapshotId",
                        column: x => x.BudgetSnapshotId,
                        principalTable: "BudgetSnapshots",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CategorySnapshotData_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategorySnapshotData_BudgetSnapshotId",
                table: "CategorySnapshotData",
                column: "BudgetSnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_CategorySnapshotData_CategoryId",
                table: "CategorySnapshotData",
                column: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategorySnapshotData");

            migrationBuilder.DropTable(
                name: "BudgetSnapshots");
        }
    }
}
