using FinancasSaaS.Application.Finance.Reports;
using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Application.Abstractions;

public interface IFinanceReportStore
{
    Task<IReadOnlyList<FinanceReportAggregate>> GetDashboardAsync(
        FinanceReportQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceReportAggregate>> GetExpensesAsync(
        FinanceReportQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceReportCompositionItem>> GetCompositionAsync(
        FinanceReportQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FinanceReportCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task<FinanceReportPreference> GetPreferenceAsync(Guid userId, CancellationToken cancellationToken = default);

    Task SavePreferenceAsync(Guid userId, ReportDateCriterion criterion, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default);
}