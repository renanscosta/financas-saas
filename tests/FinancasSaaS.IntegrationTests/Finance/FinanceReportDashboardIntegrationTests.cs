using FinancasSaaS.Application.Finance.Reports;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Platform.DeactivateCustomer;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Finance.Infrastructure.Persistence.Entities;
using FinancasSaaS.Finance.Infrastructure.InstallmentPurchases;
using FinancasSaaS.Finance.Infrastructure.ManualTransactions;
using FinancasSaaS.Finance.Infrastructure.Reports;
using FinancasSaaS.IntegrationTests.Infrastructure;
using FinancasSaaS.Platform.Infrastructure;
using FinancasSaaS.Platform.Infrastructure.Auditing;
using FinancasSaaS.Platform.Infrastructure.Configuration;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using FinancasSaaS.Platform.Infrastructure.Services;
using FinancasSaaS.Platform.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Data.Common;

namespace FinancasSaaS.IntegrationTests.Finance;

[Collection("Tenant database integration")]
public sealed class FinanceReportDashboardIntegrationTests(TenantDatabaseFixture fixture)
{
    [Theory]
    [InlineData(ReportDateCriterion.Competence, 2, 250)]
    [InlineData(ReportDateCriterion.PaymentReceipt, 3, 280)]
    public async Task ResumoIncluiParcelasEExcluiCancelados(ReportDateCriterion criterion, int expectedCount, decimal expectedTotal)
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (categoryId, _) = await ReportDataSeeder.SeedAsync(db);
        var store = new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString));

        var results = await store.GetDashboardAsync(new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), criterion));

        Assert.Equal(expectedCount, results.Sum(item => item.Count));
        Assert.Equal(expectedTotal, results.Sum(item => item.Total));
        Assert.All(results, item => Assert.Equal(categoryId, item.CategoryId));
        Assert.Equal(200m, results.Single(item => item.Type == ManualTransactionType.Income).Total);
        Assert.Equal(50m, results.Single(item => item.Type == ManualTransactionType.Expense && item.Status == ManualTransactionStatus.Planned).Total);
    }

    [Fact]
    public async Task MesVazioOuComApenasCanceladosNaoPossuiTotais()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        await ReportDataSeeder.SeedAsync(db);
        var store = new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString));

        Assert.Empty(await store.GetDashboardAsync(new FinanceReportQuery(ReportPeriod.ForMonth(2026, 11), ReportDateCriterion.Competence)));
        Assert.Empty(await store.GetDashboardAsync(new FinanceReportQuery(ReportPeriod.ForMonth(2027, 1), ReportDateCriterion.Competence)));
    }

    [Theory]
    [InlineData(ReportDateCriterion.Competence)]
    [InlineData(ReportDateCriterion.PaymentReceipt)]
    public async Task AgregacaoConfereComRegraDeDominioEmConjuntoGerado(ReportDateCriterion criterion)
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (categoryId, _) = await ReportDataSeeder.SeedAsync(db);
        for (var index = 0; index < 60; index++)
        {
            var status = index % 3 == 0 ? ManualTransactionStatus.Realized : ManualTransactionStatus.Planned;
            db.ManualTransactions.Add(new ManualTransactionEntity
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                Description = $"Gerado {index}",
                Type = index % 2 == 0 ? ManualTransactionType.Income : ManualTransactionType.Expense,
                Amount = (index + 1) * 0.01m,
                CompetenceDate = new DateOnly(2026, 9 + (index % 2), 1),
                ExpectedDate = new DateOnly(2026, 10, 10),
                Status = status,
                EffectiveDate = status == ManualTransactionStatus.Realized ? new DateOnly(2026, 11, 5) : null,
                Version = 1
            });
        }
        await db.SaveChangesAsync();
        var period = ReportPeriod.ForMonth(2026, 10);
        var all = await db.ManualTransactions.AsNoTracking().ToListAsync();
        var expected = all.Where(item =>
        {
            var reference = ReportDateRule.GetReferenceDate(criterion, item.Status, item.CompetenceDate, item.ExpectedDate, item.EffectiveDate);
            return reference is { } date && period.Contains(date, criterion);
        }).GroupBy(item => new { item.CategoryId, item.Type, item.Status })
            .ToDictionary(group => group.Key, group => (Total: group.Sum(item => item.Amount), Count: group.Count()));
        var actual = await new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString))
            .GetDashboardAsync(new FinanceReportQuery(period, criterion));

        Assert.Equal(expected.Count, actual.Count);
        foreach (var item in actual)
        {
            var expectedGroup = expected[new { item.CategoryId, item.Type, item.Status }];
            Assert.Equal(expectedGroup.Total, item.Total);
            Assert.Equal(expectedGroup.Count, item.Count);
        }
    }

    [Fact]
    public async Task FalhaDeSomaNaFronteiraDePersistenciaEhPropagada()
    {
        var store = new FinanceReportStore(new OverflowContextFactory(fixture.FirstConnectionString));
        await Assert.ThrowsAsync<OverflowException>(() => store.GetDashboardAsync(
            new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence)));
    }

    [Theory]
    [InlineData(ReportDateCriterion.Competence, 2)]
    [InlineData(ReportDateCriterion.PaymentReceipt, 1)]
    public async Task DespesasSaoAgrupadasPeloMesDeReferencia(ReportDateCriterion criterion, int expectedMonths)
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (categoryId, _) = await ReportDataSeeder.SeedAsync(db);
        var query = new FinanceReportQuery(ReportPeriod.ForRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 31)), criterion, CategoryId: categoryId);
        var results = await new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString)).GetExpensesAsync(query);

        Assert.Equal(expectedMonths, results.Select(item => item.ReferenceMonth).Distinct().Count());
        Assert.Equal(80m, results.Sum(item => item.Total));
        Assert.Equal(2, results.Sum(item => item.Count));
        Assert.All(results, item => Assert.Equal(ManualTransactionType.Expense, item.Type));
    }

    [Fact]
    public async Task CategoriasHistoricasUsamNomeAtualMesmoQuandoInativas()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (categoryId, _) = await ReportDataSeeder.SeedAsync(db);
        var category = await db.Categories.SingleAsync(item => item.Id == categoryId);
        category.Name = "Mercado renomeado";
        category.IsActive = false;
        await db.SaveChangesAsync();
        var store = new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString));

        Assert.Contains(await store.GetCategoriesAsync(), item => item.Id == categoryId && item.Name == category.Name);
        Assert.True(await store.CategoryExistsAsync(categoryId));
        Assert.False(await store.CategoryExistsAsync(Guid.NewGuid()));
        var results = await store.GetExpensesAsync(new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence));
        Assert.All(results, item => Assert.Equal(category.Name, item.CategoryName));
    }

    [Theory]
    [InlineData(ReportDateCriterion.Competence)]
    [InlineData(ReportDateCriterion.PaymentReceipt)]
    public async Task ComposicaoConciliaComAgregadoEIdentificaParcela(ReportDateCriterion criterion)
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (_, purchaseId) = await ReportDataSeeder.SeedAsync(db);
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), criterion, ManualTransactionType.Expense);
        var store = new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString));
        var aggregates = await store.GetExpensesAsync(query);
        var items = await store.GetCompositionAsync(query);

        Assert.Equal(aggregates.Sum(item => item.Total), items.Sum(item => item.Amount));
        Assert.Equal(aggregates.Sum(item => item.Count), items.Count);
        var installment = Assert.Single(items, item => item.PurchaseId == purchaseId);
        Assert.Equal(1, installment.InstallmentNumber);
        Assert.Equal(2, installment.InstallmentCount);
        Assert.Equal("Compra parcelada", installment.PurchaseDescription);
    }

    [Theory]
    [InlineData(ReportDateCriterion.Competence)]
    [InlineData(ReportDateCriterion.PaymentReceipt)]
    public async Task TodosOsRecortesDoRelatorioConciliamComComposicao(ReportDateCriterion criterion)
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (categoryId, _) = await ReportDataSeeder.SeedAsync(db);
        var store = new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString));
        var query = new FinanceReportQuery(ReportPeriod.ForRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 31)), criterion);
        var rows = await store.GetExpensesAsync(query);
        foreach (var month in new[] { 9, 10 })
        {
            var monthly = query with { Period = ReportPeriod.ForMonth(2026, month) };
            var dashboard = await store.GetDashboardAsync(monthly);
            foreach (var type in new[] { ManualTransactionType.Income, ManualTransactionType.Expense })
            {
                foreach (var status in new[] { ManualTransactionStatus.Planned, ManualTransactionStatus.Realized })
                {
                    var expected = dashboard.Where(item => item.Type == type && item.Status == status).Sum(item => item.Total);
                    var items = await store.GetCompositionAsync(monthly with { Type = type, Status = status });
                    Assert.Equal(expected, items.Sum(item => item.Amount));
                    if (type == ManualTransactionType.Expense)
                    {
                        var byCategory = await store.GetCompositionAsync(monthly with { Type = type, Status = status, CategoryId = categoryId });
                        Assert.Equal(expected, byCategory.Sum(item => item.Amount));
                    }
                }
            }
            var monthItems = await store.GetCompositionAsync(query with { Type = ManualTransactionType.Expense, ReferenceMonth = new DateOnly(2026, month, 1) });
            Assert.Equal(rows.Where(item => item.ReferenceMonth.Month == month).Sum(item => item.Total), monthItems.Sum(item => item.Amount));
            var cellItems = await store.GetCompositionAsync(query with { Type = ManualTransactionType.Expense, CategoryId = categoryId, ReferenceMonth = new DateOnly(2026, month, 1) });
            Assert.Equal(rows.Where(item => item.CategoryId == categoryId && item.ReferenceMonth.Month == month).Sum(item => item.Total), cellItems.Sum(item => item.Amount));
        }
        var categoryItems = await store.GetCompositionAsync(query with { Type = ManualTransactionType.Expense, CategoryId = categoryId });
        Assert.Equal(rows.Where(item => item.CategoryId == categoryId).Sum(item => item.Total), categoryItems.Sum(item => item.Amount));
        var allItems = await store.GetCompositionAsync(query with { Type = ManualTransactionType.Expense });
        Assert.Equal(rows.Sum(item => item.Total), allItems.Sum(item => item.Amount));
    }

    [Fact]
    public async Task PreferenciaEhIndividualEAtualizadaPorUpsert()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        var store = new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString));
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        var timestamp = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        Assert.True((await store.GetPreferenceAsync(firstUser)).IsDefault);
        await store.SavePreferenceAsync(firstUser, ReportDateCriterion.PaymentReceipt, timestamp);
        Assert.Equal(ReportDateCriterion.PaymentReceipt, (await store.GetPreferenceAsync(firstUser)).Criterion);
        Assert.True((await store.GetPreferenceAsync(secondUser)).IsDefault);
        await store.SavePreferenceAsync(secondUser, ReportDateCriterion.PaymentReceipt, timestamp);
        await store.SavePreferenceAsync(firstUser, ReportDateCriterion.Competence, DateTimeOffset.UtcNow);
        Assert.Equal(ReportDateCriterion.Competence, (await store.GetPreferenceAsync(firstUser)).Criterion);
        Assert.False((await store.GetPreferenceAsync(firstUser)).IsDefault);
        Assert.Equal(ReportDateCriterion.PaymentReceipt, (await store.GetPreferenceAsync(secondUser)).Criterion);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var saved = await db.FinanceReportPreferences.AsNoTracking().SingleAsync(item => item.UserId == secondUser);
        Assert.Equal(secondUser, saved.UpdatedByUserId);
        Assert.Equal(timestamp, saved.UpdatedAtUtc);
    }

    [Fact]
    public async Task ConsultasECategoriasPermanecemNoBancoDoTenant()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await TenantDatabaseFixture.ResetTenantAsync(fixture.SecondConnectionString);
        await using var firstDb = CreateContext(fixture.FirstConnectionString);
        await using var secondDb = CreateContext(fixture.SecondConnectionString);
        var (firstCategory, _) = await ReportDataSeeder.SeedAsync(firstDb);
        var (secondCategory, _) = await ReportDataSeeder.SeedAsync(secondDb);
        var firstStore = new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString));
        var secondStore = new FinanceReportStore(new TestContextFactory(fixture.SecondConnectionString));
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.PaymentReceipt);

        Assert.All(await firstStore.GetDashboardAsync(query), item => Assert.Equal(firstCategory, item.CategoryId));
        Assert.All(await secondStore.GetDashboardAsync(query), item => Assert.Equal(secondCategory, item.CategoryId));
        Assert.All(await firstStore.GetExpensesAsync(query), item => Assert.Equal(firstCategory, item.CategoryId));
        Assert.All(await secondStore.GetExpensesAsync(query), item => Assert.Equal(secondCategory, item.CategoryId));
        Assert.All(await firstStore.GetCompositionAsync(query), item => Assert.Equal(firstCategory, item.CategoryId));
        Assert.All(await secondStore.GetCompositionAsync(query), item => Assert.Equal(secondCategory, item.CategoryId));
        Assert.False(await firstStore.CategoryExistsAsync(secondCategory));
        var service = new FinanceReportService(firstStore, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance);
        var otherTenant = await Assert.ThrowsAsync<ArgumentException>(() => service.GetExpensesAsync(query with { CategoryId = secondCategory }));
        var nonexistent = await Assert.ThrowsAsync<ArgumentException>(() => service.GetExpensesAsync(query with { CategoryId = Guid.NewGuid() }));
        Assert.Equal(nonexistent.Message, otherTenant.Message);
        var userId = Guid.NewGuid();
        await firstStore.SavePreferenceAsync(userId, ReportDateCriterion.PaymentReceipt, DateTimeOffset.UtcNow);
        Assert.True((await secondStore.GetPreferenceAsync(userId)).IsDefault);
    }

    [Fact]
    public async Task FiltrosDeDespesasRespeitamDatasExatasEOs24Meses()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (categoryId, _) = await ReportDataSeeder.SeedAsync(db);
        var store = new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString));
        var exactPeriod = ReportPeriod.ForRange(new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 15));
        var exact = new FinanceReportQuery(exactPeriod, ReportDateCriterion.PaymentReceipt,
            Status: ManualTransactionStatus.Realized, CategoryId: categoryId);
        var realized = Assert.Single(await store.GetExpensesAsync(exact));
        Assert.Equal(30m, realized.Total);
        var planned = Assert.Single(await store.GetExpensesAsync(exact with { Status = ManualTransactionStatus.Planned }));
        Assert.Equal(50m, planned.Total);
        Assert.Empty(await store.GetExpensesAsync(exact with
        {
            Period = ReportPeriod.ForRange(new DateOnly(2026, 10, 3), new DateOnly(2026, 10, 14))
        }));
        var competence = await store.GetExpensesAsync(exact with { Criterion = ReportDateCriterion.Competence });
        Assert.Empty(competence);
        var longPeriod = ReportPeriod.ForRange(new DateOnly(2024, 11, 1), new DateOnly(2026, 10, 31));
        var all = await store.GetExpensesAsync(new FinanceReportQuery(longPeriod, ReportDateCriterion.Competence, CategoryId: categoryId));
        Assert.Equal(80m, all.Sum(item => item.Total));
        Assert.Equal(2, all.Sum(item => item.Count));
    }

    [Fact]
    public async Task RelatorioAgrupaCategoriasDiferentesEmMesesDiferentes()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (firstCategory, _) = await ReportDataSeeder.SeedAsync(db);
        var secondCategory = Guid.NewGuid();
        db.Categories.Add(new FinancasSaaS.Finance.Infrastructure.Persistence.Entities.Category { Id = secondCategory, Name = "Moradia", IsActive = true });
        db.ManualTransactions.Add(new ManualTransactionEntity
        {
            Id = Guid.NewGuid(),
            CategoryId = secondCategory,
            Description = "Aluguel",
            Type = ManualTransactionType.Expense,
            Amount = 75m,
            Status = ManualTransactionStatus.Planned,
            CompetenceDate = new DateOnly(2026, 9, 1),
            ExpectedDate = new DateOnly(2026, 10, 15),
            Version = 1
        });
        await db.SaveChangesAsync();
        var store = new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString));
        var query = new FinanceReportQuery(ReportPeriod.ForRange(new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 31)), ReportDateCriterion.Competence);
        var results = await store.GetExpensesAsync(query);
        Assert.Equal(3, results.Count);
        Assert.Contains(results, item => item.CategoryId == firstCategory && item.ReferenceMonth.Month == 10 && item.Total == 50m);
        Assert.Contains(results, item => item.CategoryId == secondCategory && item.ReferenceMonth.Month == 9 && item.Total == 75m);
        Assert.Equal(155m, results.Sum(item => item.Total));
        var filtered = await store.GetExpensesAsync(query with { CategoryId = secondCategory, Status = ManualTransactionStatus.Planned });
        Assert.Equal(75m, Assert.Single(filtered).Total);
    }

    [Fact]
    public async Task ConsultasNaoAlteramLancamentosComprasOuCategorias()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        await ReportDataSeeder.SeedAsync(db);
        var beforeTransactions = await db.ManualTransactions.AsNoTracking().OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Type, item.Description, item.Amount, item.Status, item.Version, item.CategoryId, item.CompetenceDate, item.ExpectedDate, item.EffectiveDate, item.StatusBeforeCancellation, item.InstallmentPurchaseId, item.InstallmentNumber }).ToArrayAsync();
        var beforePurchases = await db.InstallmentPurchases.AsNoTracking().OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Description, item.TotalAmount, item.Version, item.CategoryId, item.InstallmentCount, item.OriginalFirstDueDate, item.CreatedAt }).ToArrayAsync();
        var beforeCategories = await db.Categories.AsNoTracking().OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Name, item.NormalizedName, item.IsActive }).ToArrayAsync();
        var store = new FinanceReportStore(new TestContextFactory(fixture.FirstConnectionString));
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence);
        await store.GetDashboardAsync(query);
        await store.GetExpensesAsync(query);
        await store.GetCompositionAsync(query);
        await store.GetCategoriesAsync();

        Assert.Equal(beforeTransactions, await db.ManualTransactions.AsNoTracking().OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Type, item.Description, item.Amount, item.Status, item.Version, item.CategoryId, item.CompetenceDate, item.ExpectedDate, item.EffectiveDate, item.StatusBeforeCancellation, item.InstallmentPurchaseId, item.InstallmentNumber }).ToArrayAsync());
        Assert.Equal(beforePurchases, await db.InstallmentPurchases.AsNoTracking().OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Description, item.TotalAmount, item.Version, item.CategoryId, item.InstallmentCount, item.OriginalFirstDueDate, item.CreatedAt }).ToArrayAsync());
        Assert.Equal(beforeCategories, await db.Categories.AsNoTracking().OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.Name, item.NormalizedName, item.IsActive }).ToArrayAsync());
    }

    [Fact]
    public async Task RealizacaoDeParcelaMoveValorParaMesEfetivoECancelamentoRemoveAvulso()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (_, purchaseId) = await ReportDataSeeder.SeedAsync(db);
        var transactionId = await db.ManualTransactions.Where(item => item.InstallmentPurchaseId == null && item.Status == ManualTransactionStatus.Planned)
            .Select(item => item.Id).SingleAsync();
        var factory = new TestContextFactory(fixture.FirstConnectionString);
        var reports = new FinanceReportStore(factory);
        var october = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.PaymentReceipt,
            ManualTransactionType.Expense, ManualTransactionStatus.Planned);
        Assert.Equal(50m, (await reports.GetCompositionAsync(october)).Sum(item => item.Amount));

        var updated = await new InstallmentPurchaseStore(factory).RealizeAsync(purchaseId, 1, new DateOnly(2026, 11, 3), 1,
            DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.NotNull(updated);
        Assert.Empty(await reports.GetCompositionAsync(october));
        var reportService = new FinanceReportService(reports, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance);
        var refreshed = await reportService.GetCompositionAsync(october, 50m);
        Assert.True(refreshed.DataUpdated);
        Assert.Equal(refreshed.Items.Sum(item => item.Amount), refreshed.Total);
        var november = october with { Period = ReportPeriod.ForMonth(2026, 11), Status = ManualTransactionStatus.Realized };
        Assert.Equal(50m, (await reports.GetCompositionAsync(november)).Sum(item => item.Amount));
        var cancelled = await new ManualTransactionStore(factory).CancelAsync(transactionId, "Teste de cancelamento", 1,
            DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.NotNull(cancelled);
        var income = october with { Type = ManualTransactionType.Income };
        Assert.Empty(await reports.GetCompositionAsync(income));
    }

    [Fact]
    public async Task CancelamentoDaCompraRemoveParcelaDosRelatorios()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        var (_, purchaseId) = await ReportDataSeeder.SeedAsync(db);
        var factory = new TestContextFactory(fixture.FirstConnectionString);
        var reports = new FinanceReportStore(factory);
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence,
            ManualTransactionType.Expense, ManualTransactionStatus.Planned);
        Assert.Equal(50m, (await reports.GetCompositionAsync(query)).Sum(item => item.Amount));
        var cancelled = await new InstallmentPurchaseStore(factory).CancelAsync(purchaseId, new DateOnly(2026, 10, 1),
            "Cancelamento de teste", 1, DateTimeOffset.UtcNow, Guid.NewGuid());
        Assert.NotNull(cancelled);
        Assert.Empty(await reports.GetCompositionAsync(query));
        Assert.Empty(await reports.GetExpensesAsync(query));
    }

    [Fact]
    public async Task AcessoRevogadoDuranteSessaoImpedeConsultasEGravacao()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var db = CreateContext(fixture.FirstConnectionString);
        await ReportDataSeeder.SeedAsync(db);
        var resolver = new SwitchableResolver(fixture.FirstConnectionString);
        var user = new ResolverUser(resolver);
        var store = new FinanceReportStore(new TenantFinanceContextFactory(user, resolver));
        var service = new FinanceReportService(store, user, new TestClock(), NullLogger<FinanceReportService>.Instance);
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence);
        await service.SavePreferenceAsync(ReportDateCriterion.PaymentReceipt);
        Assert.True((await service.GetDashboardAsync(query)).HasData);
        resolver.Enabled = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDashboardAsync(query));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetExpensesAsync(query));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetCompositionAsync(query, 0m));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SavePreferenceAsync(ReportDateCriterion.Competence));
        var preference = await db.FinanceReportPreferences.AsNoTracking().SingleAsync();
        Assert.Equal(ReportDateCriterion.PaymentReceipt, preference.Criterion);
    }

    [Fact]
    public async Task DesativacaoRealImpedeConsultasEPreservaPreferenciaSalva()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await using var tenantDb = CreateContext(fixture.FirstConnectionString);
        await ReportDataSeeder.SeedAsync(tenantDb);

        var platformConnection = IntegrationTestConnections.Platform;
        var platformOptions = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(platformConnection).Options;
        await using var platformDb = new PlatformDbContext(platformOptions);
        await platformDb.Database.MigrateAsync();

        var customerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var databaseName = $"finance-report-{tenantId:N}";
        var now = DateTimeOffset.UtcNow;
        var email = $"finance-report-{userId:N}@example.com";
        platformDb.Customers.Add(new CustomerEntity
        {
            Id = customerId,
            Email = email,
            TenantId = tenantId,
            Status = FinancasSaaS.Domain.Platform.CustomerStatus.Active,
            CreatedAt = now
        });
        platformDb.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            CustomerId = customerId,
            DatabaseName = databaseName,
            ConnectionSecretReference = databaseName,
            Status = FinancasSaaS.Domain.Platform.CustomerStatus.Active,
            ProvisioningStatus = FinancasSaaS.Domain.Platform.ProvisioningStatus.Ready,
            CreatedAt = now
        });
        platformDb.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            TenantId = tenantId,
            AccessEnabled = true
        });
        await platformDb.SaveChangesAsync();

        var tenantOptions = new TenantDatabaseOptions
        {
            Connections = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [databaseName] = fixture.FirstConnectionString
            }
        };
        var resolver = new PlatformTenantConnectionResolver(platformDb, Options.Create(tenantOptions));
        var currentUser = new PlatformResolverUser(userId, resolver);
        var store = new FinanceReportStore(new TenantFinanceContextFactory(currentUser, resolver));
        var clock = new SystemClock();
        var service = new FinanceReportService(store, currentUser, clock, NullLogger<FinanceReportService>.Instance);
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence);

        await service.SavePreferenceAsync(ReportDateCriterion.PaymentReceipt);
        Assert.True((await service.GetDashboardAsync(query)).HasData);
        Assert.True((await service.GetExpensesAsync(query)).HasData);

        var deactivation = new DeactivateCustomerUseCase(
            new PlatformCustomerDeactivationStore(platformDb, new PlatformAuditWriter(platformDb, clock)), clock);
        var result = await deactivation.ExecuteAsync(
            new DeactivateCustomerCommand(customerId, "report-test-operator", "Teste de revogação de acesso"));
        Assert.NotNull(result);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDashboardAsync(query));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetExpensesAsync(query));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetCompositionAsync(query, 0m));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetPreferenceAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SavePreferenceAsync(ReportDateCriterion.Competence));

        var savedPreference = await tenantDb.FinanceReportPreferences.AsNoTracking().SingleAsync();
        Assert.Equal(userId, savedPreference.UserId);
        Assert.Equal(ReportDateCriterion.PaymentReceipt, savedPreference.Criterion);
    }

    private static TenantDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connectionString).Options);

    private sealed class TestContextFactory(string connectionString) : ITenantFinanceContextFactory
    {
        public Task<TenantDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateContext(connectionString));
    }

    private sealed class TestUser : ICurrentUserContext
    {
        public Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(Guid.NewGuid());
        public Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(Guid.NewGuid());
    }

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class SwitchableResolver(string connectionString) : ITenantConnectionResolver
    {
        public bool Enabled { get; set; } = true;
        public Task<string?> ResolveAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Enabled ? connectionString : null);
        public Task<Guid?> ResolveTenantIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(Enabled ? userId : null);
    }

    private sealed class PlatformResolverUser(Guid userId, ITenantConnectionResolver resolver) : ICurrentUserContext
    {
        public Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(userId);
        public Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default) => resolver.ResolveTenantIdAsync(userId, cancellationToken);
    }

    private sealed class ResolverUser(SwitchableResolver resolver) : ICurrentUserContext
    {
        private readonly Guid userId = Guid.NewGuid();
        public Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(userId);
        public Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default) => resolver.ResolveTenantIdAsync(userId, cancellationToken);
    }

    private sealed class OverflowContextFactory(string connectionString) : ITenantFinanceContextFactory
    {
        public Task<TenantDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new TenantDbContext(new DbContextOptionsBuilder<TenantDbContext>()
                .UseNpgsql(connectionString).AddInterceptors(new OverflowInterceptor()).Options));
    }

    private sealed class OverflowInterceptor : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default) => throw new OverflowException();
    }
}