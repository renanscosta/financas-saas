using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Application.Finance.Reports;

public sealed record FinanceReportQuery(
    ReportPeriod Period,
    ReportDateCriterion Criterion,
    ManualTransactionType? Type = null,
    ManualTransactionStatus? Status = null,
    Guid? CategoryId = null,
    DateOnly? ReferenceMonth = null);

public sealed record FinanceReportAggregate(
    DateOnly ReferenceMonth,
    Guid CategoryId,
    string CategoryName,
    ManualTransactionType Type,
    ManualTransactionStatus Status,
    decimal Total,
    int Count);

public sealed record FinanceReportCategory(Guid Id, string Name);

public sealed record FinanceReportCategoryTotal(Guid CategoryId, string CategoryName, decimal Planned, decimal Realized);

public sealed record FinanceDashboardReport(
    FinancialSummary Summary,
    IReadOnlyList<FinanceReportCategoryTotal> Categories,
    bool HasData);

public sealed record FinanceExpenseReportLine(
    DateOnly ReferenceMonth,
    Guid CategoryId,
    string CategoryName,
    decimal Planned,
    decimal Realized);

public sealed record FinanceReportMonthTotal(DateOnly ReferenceMonth, decimal Planned, decimal Realized);

public sealed record FinanceExpensesReport(
    IReadOnlyList<FinanceExpenseReportLine> Lines,
    IReadOnlyList<FinanceReportMonthTotal> Months,
    IReadOnlyList<FinanceReportCategoryTotal> Categories,
    decimal PlannedTotal,
    decimal RealizedTotal,
    decimal Total,
    bool HasData);

public sealed record FinanceReportCompositionItem(
    Guid Id,
    string Description,
    ManualTransactionType Type,
    Guid CategoryId,
    string CategoryName,
    decimal Amount,
    ManualTransactionStatus Status,
    DateOnly CompetenceDate,
    DateOnly ExpectedDate,
    DateOnly? EffectiveDate,
    DateOnly ReferenceDate,
    Guid? PurchaseId,
    string? PurchaseDescription,
    int? InstallmentNumber,
    int? InstallmentCount);

public sealed record FinanceReportComposition(
    IReadOnlyList<FinanceReportCompositionItem> Items,
    decimal Total,
    bool DataUpdated)
{
    public int Count => Items.Count;
}

public sealed record FinanceReportPreference(ReportDateCriterion Criterion, bool IsDefault);