using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WNAB.Logic.Data.Migrations
{
    /// <inheritdoc />
    public partial class Authentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KeycloakSubjectId",
                table: "Users",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_KeycloakSubjectId",
                table: "Users",
                column: "KeycloakSubjectId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_KeycloakSubjectId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KeycloakSubjectId",
                table: "Users");
        }
    }
}
