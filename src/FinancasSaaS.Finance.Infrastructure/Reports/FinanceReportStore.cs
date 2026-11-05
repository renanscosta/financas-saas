using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance.Reports;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Finance.Infrastructure.Reports;

public sealed class FinanceReportStore(ITenantFinanceContextFactory contextFactory) : IFinanceReportStore
{
    public async Task<IReadOnlyList<FinanceReportAggregate>> GetDashboardAsync(
        FinanceReportQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var month = new DateOnly(query.Period.Start.Year, query.Period.Start.Month, 1);
        var grouped = await ReportQueryFilter.Apply(db.ManualTransactions.AsNoTracking(), query)
            .GroupBy(item => new { item.CategoryId, CategoryName = item.Category!.Name, item.Type, item.Status })
            .Select(group => new
            {
                group.Key.CategoryId,
                group.Key.CategoryName,
                group.Key.Type,
                group.Key.Status,
                Total = group.Sum(item => item.Amount),
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        return grouped.Select(item => new FinanceReportAggregate(
            month, item.CategoryId, item.CategoryName, item.Type, item.Status, item.Total, item.Count)).ToArray();
    }

    public async Task<IReadOnlyList<FinanceReportAggregate>> GetExpensesAsync(
        FinanceReportQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var competence = query.Criterion == ReportDateCriterion.Competence;
        var grouped = await ReportQueryFilter.Apply(db.ManualTransactions.AsNoTracking(), query with { Type = ManualTransactionType.Expense })
            .Select(item => new
            {
                ReferenceDate = competence ? item.CompetenceDate :
                    item.Status == ManualTransactionStatus.Planned ? item.ExpectedDate : item.EffectiveDate!.Value,
                item.CategoryId,
                CategoryName = item.Category!.Name,
                item.Type,
                item.Status,
                item.Amount
            })
            .GroupBy(item => new { item.ReferenceDate.Year, item.ReferenceDate.Month, item.CategoryId, item.CategoryName, item.Type, item.Status })
            .Select(group => new
            {
                group.Key.Year,
                group.Key.Month,
                group.Key.CategoryId,
                group.Key.CategoryName,
                group.Key.Type,
                group.Key.Status,
                Total = group.Sum(item => item.Amount),
                Count = group.Count()
            })
            .ToListAsync(cancellationToken);

        return grouped.Select(item => new FinanceReportAggregate(
            new DateOnly(item.Year, item.Month, 1), item.CategoryId, item.CategoryName, item.Type, item.Status, item.Total, item.Count)).ToArray();
    }

    public async Task<IReadOnlyList<FinanceReportCompositionItem>> GetCompositionAsync(
        FinanceReportQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var competence = query.Criterion == ReportDateCriterion.Competence;
        return await ReportQueryFilter.Apply(db.ManualTransactions.AsNoTracking(), query)
            .OrderBy(item => item.CompetenceDate).ThenBy(item => item.Description).ThenBy(item => item.Id)
            .Select(item => new FinanceReportCompositionItem(
                item.Id, item.Description, item.Type, item.CategoryId, item.Category!.Name, item.Amount, item.Status,
                item.CompetenceDate, item.ExpectedDate, item.EffectiveDate,
                competence ? item.CompetenceDate : item.Status == ManualTransactionStatus.Planned ? item.ExpectedDate : item.EffectiveDate!.Value,
                item.InstallmentPurchaseId, item.InstallmentPurchase == null ? null : item.InstallmentPurchase.Description,
                item.InstallmentNumber, item.InstallmentPurchase == null ? null : item.InstallmentPurchase.InstallmentCount))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FinanceReportCategory>> GetCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Categories.AsNoTracking()
            .Where(category => category.IsActive || db.ManualTransactions.Any(item => item.CategoryId == category.Id))
            .OrderBy(category => category.Name)
            .Select(category => new FinanceReportCategory(category.Id, category.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Categories.AsNoTracking().AnyAsync(category => category.Id == categoryId, cancellationToken);
    }

    public async Task<FinanceReportPreference> GetPreferenceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var criterion = await db.FinanceReportPreferences.AsNoTracking().Where(item => item.UserId == userId)
            .Select(item => (ReportDateCriterion?)item.Criterion).SingleOrDefaultAsync(cancellationToken);
        return new FinanceReportPreference(criterion ?? ReportDateCriterion.Competence, criterion is null);
    }

    public async Task SavePreferenceAsync(Guid userId, ReportDateCriterion criterion, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var criterionName = criterion.ToString();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "FinanceReportPreferences" ("UserId", "Criterion", "UpdatedAtUtc", "UpdatedByUserId")
            VALUES ({userId}, {criterionName}, {updatedAtUtc.ToUniversalTime()}, {userId})
            ON CONFLICT ("UserId") DO UPDATE SET "Criterion" = EXCLUDED."Criterion",
                "UpdatedAtUtc" = EXCLUDED."UpdatedAtUtc", "UpdatedByUserId" = EXCLUDED."UpdatedByUserId"
            """, cancellationToken);
    }
}