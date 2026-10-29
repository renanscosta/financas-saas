using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancasSaaS.Finance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ManualTransactionsAndNormalizedCategories : Migration
    {
        private static readonly string[] AuditIndexColumns = ["TransactionId", "OccurredAt"];
        private static readonly string[] StatusEffectiveDateIndexColumns = ["Status", "EffectiveDate"];
        private static readonly string[] StatusExpectedDateIndexColumns = ["Status", "ExpectedDate"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "Categories"
                        GROUP BY upper(btrim("Name"))
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION 'Cannot normalize category names: equivalent legacy categories exist. Resolve them manually before retrying.';
                    END IF;
                END
                $$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                table: "Categories",
                type: "text",
                nullable: false,
                computedColumnSql: "upper(btrim(\"Name\"))",
                stored: true);

            migrationBuilder.CreateTable(
                name: "ManualTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompetenceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StatusBeforeCancellation = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManualTransactions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ManualTransactionAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChangesJson = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualTransactionAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManualTransactionAuditEvents_ManualTransactions_Transaction~",
                        column: x => x.TransactionId,
                        principalTable: "ManualTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_NormalizedName",
                table: "Categories",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.Sql(
                """
                INSERT INTO "Categories" ("Id", "Name", "IsActive")
                SELECT gen_random_uuid(), defaults."Name", TRUE
                FROM (VALUES ('Lanches'), ('Mercado'), ('Pessoal')) AS defaults("Name")
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Categories" AS category
                    WHERE category."NormalizedName" = upper(btrim(defaults."Name"))
                );

                UPDATE "Categories"
                SET "IsActive" = TRUE
                WHERE "NormalizedName" IN ('LANCHES', 'MERCADO', 'PESSOAL');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ManualTransactionAuditEvents_TransactionId_OccurredAt",
                table: "ManualTransactionAuditEvents",
                columns: AuditIndexColumns);

            migrationBuilder.CreateIndex(
                name: "IX_ManualTransactions_CategoryId",
                table: "ManualTransactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualTransactions_Status_EffectiveDate",
                table: "ManualTransactions",
                columns: StatusEffectiveDateIndexColumns);

            migrationBuilder.CreateIndex(
                name: "IX_ManualTransactions_Status_ExpectedDate",
                table: "ManualTransactions",
                columns: StatusExpectedDateIndexColumns);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualTransactionAuditEvents");

            migrationBuilder.DropTable(
                name: "ManualTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Categories_NormalizedName",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                table: "Categories");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);
        }
    }
}
