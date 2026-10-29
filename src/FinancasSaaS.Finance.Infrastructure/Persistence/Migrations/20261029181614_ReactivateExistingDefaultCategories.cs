using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancasSaaS.Finance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReactivateExistingDefaultCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "Categories"
                SET "IsActive" = TRUE
                WHERE "NormalizedName" IN ('LANCHES', 'MERCADO', 'PESSOAL');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
