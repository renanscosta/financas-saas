using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance.Reports;
using FinancasSaaS.Domain.Finance;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinancasSaaS.Application.Tests.Finance;

public sealed class FinanceReportServiceDashboardTests
{
    [Fact]
    public async Task ResumoCombinaTotaisERepassaCriterio()
    {
        var store = new TestStore();
        var service = CreateService(store, Guid.NewGuid(), Guid.NewGuid());
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.PaymentReceipt);

        var result = await service.GetDashboardAsync(query);

        Assert.Equal(query, store.LastQuery);
        Assert.True(result.HasData);
        Assert.Equal(150m, result.Summary.PlannedBalance);
        Assert.Equal(30m, Assert.Single(result.Categories).Realized);
    }

    [Fact]
    public async Task SemAtorOuTenantNaoConsultaDados()
    {
        var store = new TestStore();
        var service = CreateService(store, null, Guid.NewGuid());
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDashboardAsync(query));
        Assert.Null(store.LastQuery);
        service = CreateService(store, Guid.NewGuid(), null);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDashboardAsync(query));
        Assert.Null(store.LastQuery);
    }

    [Fact]
    public async Task FalhaDeSomaNaoEhConvertidaEmResultadoVazio()
    {
        var store = new TestStore { Failure = new OverflowException() };
        var service = CreateService(store, Guid.NewGuid(), Guid.NewGuid());

        await Assert.ThrowsAsync<OverflowException>(() => service.GetDashboardAsync(
            new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence)));
    }

    [Fact]
    public async Task FalhaEhRegistradaSemDetalhesFinanceirosDaExcecao()
    {
        var logger = new RecordingLogger();
        var service = new FinanceReportService(
            new TestStore { Failure = new OverflowException("valor 999.99 e descrição privada") },
            new TestUser(Guid.NewGuid(), Guid.NewGuid()), new TestClock(), logger);

        await Assert.ThrowsAsync<OverflowException>(() => service.GetDashboardAsync(
            new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence)));

        Assert.NotNull(logger.Message);
        Assert.Contains("report.dashboard", logger.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("999.99", logger.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("descrição privada", logger.Message, StringComparison.Ordinal);
        Assert.Null(logger.Exception);
    }

    [Fact]
    public async Task RelatorioReuneEstadosDaMesmaCategoriaERejeitaCategoriaInvalida()
    {
        var store = new TestStore();
        var service = CreateService(store, Guid.NewGuid(), Guid.NewGuid());
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence);
        var result = await service.GetExpensesAsync(query);

        Assert.Equal(50m, result.PlannedTotal);
        Assert.Equal(30m, result.RealizedTotal);
        Assert.Equal(80m, result.Total);
        Assert.Single(result.Lines);
        Assert.Equal(50m, Assert.Single(result.Months).Planned);
        Assert.Equal(30m, Assert.Single(result.Categories).Realized);
        store.LastQuery = null;
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.GetExpensesAsync(query with { CategoryId = Guid.NewGuid() }));
        Assert.Contains("Categoria inválida", exception.Message, StringComparison.Ordinal);
        Assert.Null(store.LastQuery);
    }

    [Fact]
    public async Task PreferenciaUsaSomenteAtorAutenticadoEConservaValorEmFalha()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var store = new TestStore();
        var logger = new RecordingLogger();
        var service = new FinanceReportService(store, new TestUser(actorId, tenantId), new TestClock(), logger);
        await service.SavePreferenceAsync(ReportDateCriterion.PaymentReceipt);
        Assert.Contains("report.preference", logger.Message, StringComparison.Ordinal);
        Assert.Contains(actorId.ToString(), logger.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(tenantId.ToString(), logger.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Null(logger.Exception);
        Assert.Equal(actorId, store.PreferenceUserId);
        Assert.Equal(DateTimeOffset.UnixEpoch, store.PreferenceUpdatedAt);
        Assert.Equal(ReportDateCriterion.PaymentReceipt, (await service.GetPreferenceAsync()).Criterion);
        store.PreferenceFailure = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SavePreferenceAsync(ReportDateCriterion.Competence));
        Assert.Contains("report.preference.write", logger.Message, StringComparison.Ordinal);
        Assert.Null(logger.Exception);
        Assert.Equal(ReportDateCriterion.PaymentReceipt, (await service.GetPreferenceAsync()).Criterion);
        var unauthorized = CreateService(store, null, Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidOperationException>(() => unauthorized.SavePreferenceAsync(ReportDateCriterion.Competence));
    }

    [Fact]
    public async Task ComposicaoSomaItensEIndicaAlteracaoDesdeTotalAnterior()
    {
        var date = new DateOnly(2026, 10, 1);
        var store = new TestStore
        {
            Composition = [new FinanceReportCompositionItem(Guid.NewGuid(), "Despesa", ManualTransactionType.Expense,
                Guid.NewGuid(), "Categoria", 12.34m, ManualTransactionStatus.Planned, date, date, null, date, null, null, null, null)]
        };
        var service = CreateService(store, Guid.NewGuid(), Guid.NewGuid());
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence);
        var current = await service.GetCompositionAsync(query, 12.34m);
        Assert.Equal(12.34m, current.Total);
        Assert.Equal(1, current.Count);
        Assert.False(current.DataUpdated);
        Assert.True((await service.GetCompositionAsync(query, 10m)).DataUpdated);
        store.Composition = [];
        var empty = await service.GetCompositionAsync(query, 0m);
        Assert.Empty(empty.Items);
        Assert.False(empty.DataUpdated);
    }

    [Fact]
    public async Task TotalCombinadoNaoRepresentavelEhUmaFalha()
    {
        var date = new DateOnly(2026, 10, 1);
        var category = Guid.NewGuid();
        var store = new TestStore
        {
            Expenses = [
                new(date, category, "Categoria", ManualTransactionType.Expense, ManualTransactionStatus.Planned, decimal.MaxValue, 1),
                new(date, category, "Categoria", ManualTransactionType.Expense, ManualTransactionStatus.Realized, 1m, 1)]
        };
        var service = CreateService(store, Guid.NewGuid(), Guid.NewGuid());
        await Assert.ThrowsAsync<OverflowException>(() => service.GetExpensesAsync(
            new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence)));
    }

    [Fact]
    public async Task PeriodoInvalidoNaoExecutaConsultaDeDespesas()
    {
        var store = new TestStore();
        var service = CreateService(store, Guid.NewGuid(), Guid.NewGuid());
        var first = new DateOnly(2026, 1, 1);
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetExpensesAsync(
            new FinanceReportQuery(ReportPeriod.ForRange(null, first), ReportDateCriterion.Competence)));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetExpensesAsync(
            new FinanceReportQuery(ReportPeriod.ForRange(first, first.AddDays(-1)), ReportDateCriterion.Competence)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.GetExpensesAsync(
            new FinanceReportQuery(ReportPeriod.ForRange(first, first.AddMonths(24)), ReportDateCriterion.Competence)));
        Assert.Null(store.LastQuery);
    }

    [Fact]
    public async Task LogsDeRelatorioNaoIncluemDadosSensiveis()
    {
        var logger = new RecordingLogger();
        var store = new TestStore { Failure = new InvalidOperationException("123.45 descrição categoria senha token") };
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var service = new FinanceReportService(store, new TestUser(actorId, tenantId), new TestClock(), logger);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetDashboardAsync(
            new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence)));
        Assert.Contains("report.dashboard", logger.Message, StringComparison.Ordinal);
        Assert.Contains("failure", logger.Message, StringComparison.Ordinal);
        Assert.Contains(actorId.ToString(), logger.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(tenantId.ToString(), logger.Message, StringComparison.OrdinalIgnoreCase);
        foreach (var secret in new[] { "123.45", "descrição", "categoria", "senha", "token" })
        {
            Assert.DoesNotContain(secret, logger.Message, StringComparison.OrdinalIgnoreCase);
        }
        Assert.Null(logger.Exception);

        await service.SavePreferenceAsync(ReportDateCriterion.PaymentReceipt);
        Assert.Contains("report.preference", logger.Message, StringComparison.Ordinal);
        Assert.Contains("success", logger.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("PaymentReceipt", logger.Message, StringComparison.Ordinal);
        Assert.Null(logger.Exception);
    }

    private static FinanceReportService CreateService(TestStore store, Guid? actorId, Guid? tenantId) =>
        new(store, new TestUser(actorId, tenantId), new TestClock(), NullLogger<FinanceReportService>.Instance);

    private sealed class TestUser(Guid? actorId, Guid? tenantId) : ICurrentUserContext
    {
        public Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(actorId);
        public Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(tenantId);
    }

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
    }

    private sealed class RecordingLogger : ILogger<FinanceReportService>
    {
        public string? Message { get; private set; }
        public Exception? Exception { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Message = formatter(state, exception);
            Exception = exception;
        }
    }

    private sealed class TestStore : IFinanceReportStore
    {
        public FinanceReportQuery? LastQuery { get; set; }
        public Exception? Failure { get; init; }
        public Guid? PreferenceUserId { get; private set; }
        public DateTimeOffset PreferenceUpdatedAt { get; private set; }
        public ReportDateCriterion Preference { get; private set; } = ReportDateCriterion.Competence;
        public bool PreferenceFailure { get; set; }
        public IReadOnlyList<FinanceReportCompositionItem> Composition { get; set; } = [];
        public IReadOnlyList<FinanceReportAggregate>? Expenses { get; init; }

        public Task<IReadOnlyList<FinanceReportAggregate>> GetDashboardAsync(FinanceReportQuery query, CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            if (Failure is not null)
            {
                throw Failure;
            }

            var category = Guid.NewGuid();
            IReadOnlyList<FinanceReportAggregate> result =
            [
                new(new DateOnly(2026, 10, 1), category, "Mercado", ManualTransactionType.Income, ManualTransactionStatus.Planned, 200m, 1),
                new(new DateOnly(2026, 10, 1), category, "Mercado", ManualTransactionType.Expense, ManualTransactionStatus.Planned, 50m, 1),
                new(new DateOnly(2026, 10, 1), category, "Mercado", ManualTransactionType.Expense, ManualTransactionStatus.Realized, 30m, 1)
            ];
            return Task.FromResult(result);
        }

        public async Task<IReadOnlyList<FinanceReportAggregate>> GetExpensesAsync(FinanceReportQuery query, CancellationToken cancellationToken = default) =>
            Expenses ?? (await GetDashboardAsync(query, cancellationToken)).Where(item => item.Type == ManualTransactionType.Expense).ToArray();
        public Task<IReadOnlyList<FinanceReportCompositionItem>> GetCompositionAsync(FinanceReportQuery query, CancellationToken cancellationToken = default) => Task.FromResult(Composition);
        public Task<IReadOnlyList<FinanceReportCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<FinanceReportPreference> GetPreferenceAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(new FinanceReportPreference(Preference, PreferenceUserId is null));
        public Task SavePreferenceAsync(Guid userId, ReportDateCriterion criterion, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default)
        {
            if (PreferenceFailure) throw new InvalidOperationException();
            PreferenceUserId = userId;
            PreferenceUpdatedAt = updatedAtUtc;
            Preference = criterion;
            return Task.CompletedTask;
        }
    }
}