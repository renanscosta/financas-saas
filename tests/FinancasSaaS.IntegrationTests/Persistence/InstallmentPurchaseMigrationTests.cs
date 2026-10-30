using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Finance.Infrastructure.Persistence.Entities;
using FinancasSaaS.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Persistence;

[Collection("Tenant database integration")]
public sealed class InstallmentPurchaseMigrationTests(TenantDatabaseFixture fixture)
{
    [Fact]
    public async Task AplicaMigrationEmTenantNovoERepeticaoNaoAlteraEstado()
    {
        var (tenantId, connectionString) = await fixture.CreateTemporaryTenantDatabaseAsync();
        try
        {
            await using var db = CreateContext(connectionString);
            await db.Database.MigrateAsync();
            await db.Database.MigrateAsync();

            var appliedMigrations = await db.Database.GetAppliedMigrationsAsync();
            Assert.Contains("20261029234535_InstallmentPurchasesAndAudit", appliedMigrations);
            Assert.Empty(await db.InstallmentPurchases.ToListAsync());
            Assert.Empty(await db.InstallmentPurchaseAuditEvents.ToListAsync());
        }
        finally
        {
            await fixture.DeleteTemporaryTenantDatabaseAsync(tenantId);
        }
    }

    [Fact]
    public async Task PreservaLancamentosExistentesAoAplicarMigrationFinanceira()
    {
        var (tenantId, connectionString) = await fixture.CreateTemporaryTenantDatabaseAsync();
        var categoryId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        try
        {
            await using var db = CreateContext(connectionString);
            await db.Database.MigrateAsync("20261029181614_ReactivateExistingDefaultCategories");
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
            Assert.Equal("Despesa preservada", transaction.Description);
            Assert.Equal(12.34m, transaction.Amount);
            Assert.Null(transaction.InstallmentPurchaseId);
            Assert.Equal(0, await db.InstallmentPurchases.CountAsync());
        }
        finally
        {
            await fixture.DeleteTemporaryTenantDatabaseAsync(tenantId);
        }
    }

    private static TenantDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new TenantDbContext(options);
    }
}
