using FinancasSaaS.Application.Finance.Reports;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Finance.Infrastructure.Reports;
using FinancasSaaS.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.IntegrationTests.Finance;

[Collection("Tenant database integration")]
public sealed class ReportQueryFilterTests(TenantDatabaseFixture fixture)
{
    [Theory]
    [InlineData(ReportDateCriterion.Competence, 2)]
    [InlineData(ReportDateCriterion.PaymentReceipt, 3)]
    public async Task AplicaCriterioAAvulsosEParcelasSemIncluirCancelados(ReportDateCriterion criterion, int expectedCount)
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (categoryId, _) = await ReportDataSeeder.SeedAsync(db);
        var category = await db.Categories.SingleAsync(item => item.Id == categoryId);
        category.IsActive = false;
        await db.SaveChangesAsync();

        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), criterion);
        var results = await ReportQueryFilter.Apply(db.ManualTransactions.AsNoTracking(), query).ToListAsync();

        Assert.Equal(expectedCount, results.Count);
        Assert.Contains(results, item => item.InstallmentPurchaseId != null);
        Assert.All(results, item => Assert.NotEqual(ManualTransactionStatus.Cancelled, item.Status));
    }

    [Fact]
    public async Task IntersectaMesDeReferenciaComIntervaloEAplicaFiltros()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (categoryId, _) = await ReportDataSeeder.SeedAsync(db);
        var query = new FinanceReportQuery(
            ReportPeriod.ForRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 11, 30)),
            ReportDateCriterion.PaymentReceipt,
            ManualTransactionType.Expense,
            ManualTransactionStatus.Realized,
            categoryId,
            new DateOnly(2026, 10, 1));

        var results = await ReportQueryFilter.Apply(db.ManualTransactions.AsNoTracking(), query).ToListAsync();

        var item = Assert.Single(results);
        Assert.Equal(30m, item.Amount);
        Assert.Equal(new DateOnly(2026, 10, 2), item.EffectiveDate);
    }

    private static TenantDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connectionString).Options);
}