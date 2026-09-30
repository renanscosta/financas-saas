using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancasSaaS.Platform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlatformReferentialIntegrity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Invitations_CustomerId",
                table: "Invitations",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_TenantId",
                table: "Invitations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Invitations_UserId",
                table: "Invitations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_TenantId",
                table: "AuditEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers",
                column: "TenantId");

            migrationBuilder.Sql("ALTER TABLE \"AspNetUsers\" ADD CONSTRAINT \"FK_AspNetUsers_Tenants_TenantId\" FOREIGN KEY (\"TenantId\") REFERENCES \"Tenants\" (\"Id\") ON DELETE RESTRICT NOT VALID;");
            migrationBuilder.Sql("ALTER TABLE \"AuditEntries\" ADD CONSTRAINT \"FK_AuditEntries_Tenants_TenantId\" FOREIGN KEY (\"TenantId\") REFERENCES \"Tenants\" (\"Id\") ON DELETE RESTRICT NOT VALID;");
            migrationBuilder.Sql("ALTER TABLE \"Invitations\" ADD CONSTRAINT \"FK_Invitations_AspNetUsers_UserId\" FOREIGN KEY (\"UserId\") REFERENCES \"AspNetUsers\" (\"Id\") ON DELETE RESTRICT NOT VALID;");
            migrationBuilder.Sql("ALTER TABLE \"Invitations\" ADD CONSTRAINT \"FK_Invitations_Customers_CustomerId\" FOREIGN KEY (\"CustomerId\") REFERENCES \"Customers\" (\"Id\") ON DELETE RESTRICT NOT VALID;");
            migrationBuilder.Sql("ALTER TABLE \"Invitations\" ADD CONSTRAINT \"FK_Invitations_Tenants_TenantId\" FOREIGN KEY (\"TenantId\") REFERENCES \"Tenants\" (\"Id\") ON DELETE RESTRICT NOT VALID;");
            migrationBuilder.Sql("ALTER TABLE \"Provisionings\" ADD CONSTRAINT \"FK_Provisionings_Tenants_TenantId\" FOREIGN KEY (\"TenantId\") REFERENCES \"Tenants\" (\"Id\") ON DELETE RESTRICT NOT VALID;");
            migrationBuilder.Sql("ALTER TABLE \"Tenants\" ADD CONSTRAINT \"FK_Tenants_Customers_CustomerId\" FOREIGN KEY (\"CustomerId\") REFERENCES \"Customers\" (\"Id\") ON DELETE RESTRICT NOT VALID;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Tenants_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditEntries_Tenants_TenantId",
                table: "AuditEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_AspNetUsers_UserId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Customers_CustomerId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Tenants_TenantId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Provisionings_Tenants_TenantId",
                table: "Provisionings");

            migrationBuilder.DropForeignKey(
                name: "FK_Tenants_Customers_CustomerId",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_CustomerId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_TenantId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_Invitations_UserId",
                table: "Invitations");

            migrationBuilder.DropIndex(
                name: "IX_AuditEntries_TenantId",
                table: "AuditEntries");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers");
        }
    }
}
