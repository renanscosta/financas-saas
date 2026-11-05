using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinancasSaaS.IntegrationTests.Persistence;

[Collection("Tenant database integration")]
public sealed class FinanceReportMigrationTests(TenantDatabaseFixture fixture)
{
    [Fact]
    public async Task MigrationNovaERepetidaCriaPreferenciaEIndice()
    {
        var (tenantId, connectionString) = await fixture.CreateTemporaryTenantDatabaseAsync();
        try
        {
            await using var db = CreateContext(connectionString);
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();

            Assert.Contains("20261031040407_FinanceReportPreference", await db.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await db.FinanceReportPreferences.ToListAsync());
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT count(*) FROM pg_indexes WHERE tablename = 'ManualTransactions' AND indexname = 'IX_ManualTransactions_Status_CompetenceDate'";
            Assert.Equal(1L, (long)(await command.ExecuteScalarAsync())!);
        }
        finally
        {
            await fixture.DeleteTemporaryTenantDatabaseAsync(tenantId);
        }
    }

    [Fact]
    public async Task MigrationPreservaLancamentosExistentes()
    {
        var (tenantId, connectionString) = await fixture.CreateTemporaryTenantDatabaseAsync();
        var categoryId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        try
        {
            await using var db = CreateContext(connectionString);
            await db.Database.MigrateAsync("20261029234535_InstallmentPurchasesAndAudit");
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "Categories" ("Id", "Name", "IsActive")
                VALUES ({categoryId}, {"Categoria existente"}, {true});
                """);
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO "ManualTransactions"
                    ("Id", "Type", "Description", "Amount", "CategoryId", "CompetenceDate",
                     "ExpectedDate", "Status", "EffectiveDate", "StatusBeforeCancellation", "Version")
                VALUES
                    ({transactionId}, {"Expense"}, {"Despesa preservada"}, {12.34m}, {categoryId},
                     {new DateOnly(2026, 1, 1)}, {new DateOnly(2026, 1, 15)}, {"Planned"},
                     {null}, {null}, {1L});
                """);

            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();

            var transaction = await db.ManualTransactions.AsNoTracking().SingleAsync(item => item.Id == transactionId);
            Assert.Equal(12.34m, transaction.Amount);
            Assert.Equal("Despesa preservada", transaction.Description);
        }
        finally
        {
            await fixture.DeleteTemporaryTenantDatabaseAsync(tenantId);
        }
    }

    private static TenantDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connectionString).Options;
        return new TenantDbContext(options);
    }
}