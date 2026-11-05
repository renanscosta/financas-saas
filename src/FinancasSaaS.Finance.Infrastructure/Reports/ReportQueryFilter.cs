using FinancasSaaS.Application.Finance.Reports;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Finance.Infrastructure.Persistence.Entities;

namespace FinancasSaaS.Finance.Infrastructure.Reports;

public static class ReportQueryFilter
{
    public static IQueryable<ManualTransactionEntity> Apply(
        IQueryable<ManualTransactionEntity> transactions,
        FinanceReportQuery query)
    {
        ArgumentNullException.ThrowIfNull(transactions);
        ArgumentNullException.ThrowIfNull(query);

        var start = query.Period.Start;
        var end = query.Period.End;
        if (query.Criterion == ReportDateCriterion.Competence)
        {
            start = ReportPeriod.ForMonth(start.Year, start.Month).Start;
            end = ReportPeriod.ForMonth(end.Year, end.Month).End;
        }
        else if (query.Criterion != ReportDateCriterion.PaymentReceipt)
        {
            throw new ArgumentOutOfRangeException(nameof(query));
        }

        if (query.ReferenceMonth is { } referenceMonth)
        {
            var month = ReportPeriod.ForMonth(referenceMonth.Year, referenceMonth.Month);
            start = start > month.Start ? start : month.Start;
            end = end < month.End ? end : month.End;
        }

        transactions = transactions.Where(transaction =>
            transaction.Status == ManualTransactionStatus.Planned || transaction.Status == ManualTransactionStatus.Realized);

        transactions = query.Criterion == ReportDateCriterion.Competence
            ? transactions.Where(transaction => transaction.CompetenceDate >= start && transaction.CompetenceDate <= end)
            : transactions.Where(transaction =>
                (transaction.Status == ManualTransactionStatus.Planned && transaction.ExpectedDate >= start && transaction.ExpectedDate <= end) ||
                (transaction.Status == ManualTransactionStatus.Realized && transaction.EffectiveDate >= start && transaction.EffectiveDate <= end));

        if (query.Type is { } type)
        {
            transactions = transactions.Where(transaction => transaction.Type == type);
        }

        if (query.Status is { } status)
        {
            transactions = transactions.Where(transaction => transaction.Status == status);
        }

        if (query.CategoryId is { } categoryId)
        {
            transactions = transactions.Where(transaction => transaction.CategoryId == categoryId);
        }

        return transactions;
    }
}