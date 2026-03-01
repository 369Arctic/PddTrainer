using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PddTrainer.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangePropertyType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE ""Attempts"" 
                ALTER COLUMN ""Passed"" TYPE boolean 
                USING ""Passed""::boolean;"
                );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE ""Attempts"" 
                ALTER COLUMN ""Passed"" TYPE integer 
                USING CASE WHEN ""Passed"" THEN 1 ELSE 0 END;"
                );
        }
    }
}
