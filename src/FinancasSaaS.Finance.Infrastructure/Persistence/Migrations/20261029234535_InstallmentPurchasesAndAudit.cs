using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancasSaaS.Finance.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InstallmentPurchasesAndAudit : Migration
    {
        private static readonly string[] InstallmentIndexColumns = ["InstallmentPurchaseId", "InstallmentNumber"];
        private static readonly string[] AuditIndexColumns = ["PurchaseId", "OccurredAt"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstallmentNumber",
                table: "ManualTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstallmentPurchaseId",
                table: "ManualTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InstallmentPurchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InstallmentCount = table.Column<int>(type: "integer", nullable: false),
                    OriginalFirstDueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallmentPurchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstallmentPurchases_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InstallmentPurchaseAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ChangesJson = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstallmentPurchaseAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstallmentPurchaseAuditEvents_InstallmentPurchases_Purchas~",
                        column: x => x.PurchaseId,
                        principalTable: "InstallmentPurchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManualTransactions_InstallmentPurchaseId_InstallmentNumber",
                table: "ManualTransactions",
                columns: InstallmentIndexColumns,
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPurchaseAuditEvents_PurchaseId_OccurredAt",
                table: "InstallmentPurchaseAuditEvents",
                columns: AuditIndexColumns);

            migrationBuilder.CreateIndex(
                name: "IX_InstallmentPurchases_CategoryId",
                table: "InstallmentPurchases",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ManualTransactions_InstallmentPurchases_InstallmentPurchase~",
                table: "ManualTransactions",
                column: "InstallmentPurchaseId",
                principalTable: "InstallmentPurchases",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ManualTransactions_InstallmentPurchases_InstallmentPurchase~",
                table: "ManualTransactions");

            migrationBuilder.DropTable(
                name: "InstallmentPurchaseAuditEvents");

            migrationBuilder.DropTable(
                name: "InstallmentPurchases");

            migrationBuilder.DropIndex(
                name: "IX_ManualTransactions_InstallmentPurchaseId_InstallmentNumber",
                table: "ManualTransactions");

            migrationBuilder.DropColumn(
                name: "InstallmentNumber",
                table: "ManualTransactions");

            migrationBuilder.DropColumn(
                name: "InstallmentPurchaseId",
                table: "ManualTransactions");
        }
    }
}
