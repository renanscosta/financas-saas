using System.Diagnostics;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Domain.Finance;
using Microsoft.Extensions.Logging;

namespace FinancasSaaS.Application.Finance.Reports;

public sealed class FinanceReportService(
    IFinanceReportStore store,
    ICurrentUserContext currentUser,
    ISystemClock clock,
    ILogger<FinanceReportService> logger)
{
    internal IFinanceReportStore Store { get; } = store;

    public async Task<FinanceDashboardReport> GetDashboardAsync(FinanceReportQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var (actorId, tenantId) = await RequireContextAsync(cancellationToken);
        try
        {
            var aggregates = await Store.GetDashboardAsync(query, cancellationToken);
            var summary = new FinancialSummary(
                aggregates.Where(item => item.Type == ManualTransactionType.Income && item.Status == ManualTransactionStatus.Planned).Sum(item => item.Total),
                aggregates.Where(item => item.Type == ManualTransactionType.Income && item.Status == ManualTransactionStatus.Realized).Sum(item => item.Total),
                aggregates.Where(item => item.Type == ManualTransactionType.Expense && item.Status == ManualTransactionStatus.Planned).Sum(item => item.Total),
                aggregates.Where(item => item.Type == ManualTransactionType.Expense && item.Status == ManualTransactionStatus.Realized).Sum(item => item.Total));
            var categories = aggregates.Where(item => item.Type == ManualTransactionType.Expense)
                .GroupBy(item => new { item.CategoryId, item.CategoryName })
                .Select(group => new FinanceReportCategoryTotal(
                    group.Key.CategoryId,
                    group.Key.CategoryName,
                    group.Where(item => item.Status == ManualTransactionStatus.Planned).Sum(item => item.Total),
                    group.Where(item => item.Status == ManualTransactionStatus.Realized).Sum(item => item.Total)))
                .OrderBy(item => item.CategoryName, StringComparer.CurrentCulture)
                .ToArray();
            return new FinanceDashboardReport(summary, categories, aggregates.Count > 0);
        }
        catch (Exception)
        {
            LogFailure("report.dashboard", actorId, tenantId);
            throw;
        }
    }

    public async Task<FinanceExpensesReport> GetExpensesAsync(FinanceReportQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var (actorId, tenantId) = await RequireContextAsync(cancellationToken);
        try
        {
            if (query.CategoryId is { } categoryId && !await Store.CategoryExistsAsync(categoryId, cancellationToken))
            {
                throw new ArgumentException("Categoria inválida.", nameof(query));
            }

            var aggregates = await Store.GetExpensesAsync(query, cancellationToken);
            var lines = aggregates.GroupBy(item => new { item.ReferenceMonth, item.CategoryId, item.CategoryName })
                .Select(group => new FinanceExpenseReportLine(
                    group.Key.ReferenceMonth, group.Key.CategoryId, group.Key.CategoryName,
                    group.Where(item => item.Status == ManualTransactionStatus.Planned).Sum(item => item.Total),
                    group.Where(item => item.Status == ManualTransactionStatus.Realized).Sum(item => item.Total)))
                .OrderBy(item => item.ReferenceMonth).ThenBy(item => item.CategoryName, StringComparer.CurrentCulture)
                .ToArray();
            var months = lines.GroupBy(item => item.ReferenceMonth)
                .Select(group => new FinanceReportMonthTotal(group.Key, group.Sum(item => item.Planned), group.Sum(item => item.Realized)))
                .ToArray();
            var categories = lines.GroupBy(item => new { item.CategoryId, item.CategoryName })
                .Select(group => new FinanceReportCategoryTotal(group.Key.CategoryId, group.Key.CategoryName,
                    group.Sum(item => item.Planned), group.Sum(item => item.Realized)))
                .OrderBy(item => item.CategoryName, StringComparer.CurrentCulture).ToArray();
            var plannedTotal = lines.Sum(item => item.Planned);
            var realizedTotal = lines.Sum(item => item.Realized);
            return new FinanceExpensesReport(lines, months, categories, plannedTotal, realizedTotal, plannedTotal + realizedTotal, lines.Length > 0);
        }
        catch (Exception)
        {
            LogFailure("report.expenses", actorId, tenantId);
            throw;
        }
    }

    public async Task<IReadOnlyList<FinanceReportCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var (actorId, tenantId) = await RequireContextAsync(cancellationToken);
        try
        {
            return await Store.GetCategoriesAsync(cancellationToken);
        }
        catch (Exception)
        {
            LogFailure("report.categories", actorId, tenantId);
            throw;
        }
    }

    public async Task<FinanceReportComposition> GetCompositionAsync(FinanceReportQuery query, decimal expectedTotal, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var (actorId, tenantId) = await RequireContextAsync(cancellationToken);
        try
        {
            if (query.CategoryId is { } categoryId && !await Store.CategoryExistsAsync(categoryId, cancellationToken))
            {
                throw new ArgumentException("Categoria inválida.", nameof(query));
            }
            var items = await Store.GetCompositionAsync(query, cancellationToken);
            var total = items.Sum(item => item.Amount);
            return new FinanceReportComposition(items, total, total != expectedTotal);
        }
        catch (Exception)
        {
            LogFailure("report.composition", actorId, tenantId);
            throw;
        }
    }

    public async Task<FinanceReportPreference> GetPreferenceAsync(CancellationToken cancellationToken = default)
    {
        var (actorId, tenantId) = await RequireContextAsync(cancellationToken);
        try
        {
            return await Store.GetPreferenceAsync(actorId, cancellationToken);
        }
        catch (Exception)
        {
            LogFailure("report.preference.read", actorId, tenantId);
            throw;
        }
    }

    public async Task SavePreferenceAsync(ReportDateCriterion criterion, CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(criterion)) throw new ArgumentOutOfRangeException(nameof(criterion));
        var (actorId, tenantId) = await RequireContextAsync(cancellationToken);
        try
        {
            await Store.SavePreferenceAsync(actorId, criterion, clock.UtcNow.ToUniversalTime(), cancellationToken);
            LogPreferenceChange(actorId, tenantId);
        }
        catch (Exception)
        {
            LogFailure("report.preference.write", actorId, tenantId);
            throw;
        }
    }

    internal async Task<(Guid ActorId, Guid TenantId)> RequireContextAsync(CancellationToken cancellationToken)
    {
        var actorId = await currentUser.GetUserIdAsync(cancellationToken);
        var tenantId = await currentUser.GetTenantIdAsync(cancellationToken);
        if (actorId is not Guid actor || actor == Guid.Empty || tenantId is not Guid tenant || tenant == Guid.Empty)
        {
            throw new InvalidOperationException("O acesso ao tenant autenticado não está disponível.");
        }

        return (actor, tenant);
    }

    internal void LogFailure(string operation, Guid actorId, Guid tenantId)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            FinanceLog.ReportFailed(logger, operation, "failure", actorId, tenantId, clock.UtcNow.ToUniversalTime(), Activity.Current?.TraceId.ToString());
        }
    }

    internal void LogPreferenceChange(Guid actorId, Guid tenantId)
    {
        var occurredAtUtc = clock.UtcNow.ToUniversalTime();
        var correlationId = Activity.Current?.TraceId.ToString();
        if (logger.IsEnabled(LogLevel.Information))
        {
            FinanceLog.OperationCompleted(logger, "report.preference", "success", actorId, tenantId, occurredAtUtc, correlationId);
        }
    }
}