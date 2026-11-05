using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancasSaaS.Finance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinanceReportPreference : Migration
    {
        private static readonly string[] CompetenceIndexColumns = ["Status", "CompetenceDate"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceReportPreferences",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Criterion = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceReportPreferences", x => x.UserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManualTransactions_Status_CompetenceDate",
                table: "ManualTransactions",
                columns: CompetenceIndexColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceReportPreferences");

            migrationBuilder.DropIndex(
                name: "IX_ManualTransactions_Status_CompetenceDate",
                table: "ManualTransactions");
        }
    }
}
