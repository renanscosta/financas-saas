using System.Diagnostics;
using FinancasSaaS.Application.Finance.Reports;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Finance.Infrastructure.Persistence.Entities;
using FinancasSaaS.Finance.Infrastructure.Reports;
using FinancasSaaS.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace FinancasSaaS.IntegrationTests.Performance;

[Collection("Tenant database integration")]
public sealed class FinanceReportPerformanceTests(TenantDatabaseFixture fixture, ITestOutputHelper output)
{
    [Fact]
    public async Task ResumoDeCincoMilLancamentos()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (categoryId, _) = await ReportDataSeeder.SeedAsync(db);
        for (var index = 0; index < 5000; index++)
        {
            db.ManualTransactions.Add(new ManualTransactionEntity
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                Description = $"Teste {index}",
                Type = ManualTransactionType.Expense,
                Status = ManualTransactionStatus.Planned,
                Amount = 1m,
                CompetenceDate = new DateOnly(2026, 10, 1),
                ExpectedDate = new DateOnly(2026, 10, 15),
                Version = 1
            });
        }
        await db.SaveChangesAsync();
        var store = new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString));
        var stopwatch = Stopwatch.StartNew();
        var result = await store.GetDashboardAsync(new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence));
        stopwatch.Stop();
        output.WriteLine("Agregação SQL de 5.000 lançamentos: {0} ms; ambiente: {1}; processadores: {2}.",
            stopwatch.ElapsedMilliseconds, Environment.Version, Environment.ProcessorCount);
        Assert.Equal(5002, result.Sum(item => item.Count));
        Assert.Equal(5250m, result.Sum(item => item.Total));
    }

    private static TenantDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connectionString).Options);

    private sealed class TestContextFactory(string connectionString) : ITenantFinanceContextFactory
    {
        public Task<TenantDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateContext(connectionString));
    }
}