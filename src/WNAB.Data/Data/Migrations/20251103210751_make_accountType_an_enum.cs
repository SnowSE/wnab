using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WNAB.Data.Data.Migrations
{
    /// <inheritdoc />
    public partial class make_accountType_an_enum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Convert string values to integer enum values using SQL USING clause
            migrationBuilder.Sql(@"
        ALTER TABLE ""Accounts"" 
    ALTER COLUMN ""AccountType"" TYPE integer 
        USING (
            CASE ""AccountType""
      WHEN 'Checking' THEN 0
              WHEN 'Savings' THEN 1
              WHEN 'Misc' THEN 2
              ELSE 0
            END
                );
    ");
        }

  /// <inheritdoc />
   protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Convert integer enum values back to string
         migrationBuilder.Sql(@"
 ALTER TABLE ""Accounts"" 
       ALTER COLUMN ""AccountType"" TYPE character varying(50) 
            USING (
      CASE ""AccountType""
        WHEN 0 THEN 'Checking'
   WHEN 1 THEN 'Savings'
     WHEN 2 THEN 'Misc'
       ELSE 'Checking'
   END
         );
            ");
        }
}
}
