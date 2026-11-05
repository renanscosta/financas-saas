using System.Globalization;
using FinancasSaaS.Domain.Finance;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace FinancasSaaS.Web.Components;

public sealed record ReportQueryParameters(
    DateOnly Month,
    DateOnly? Start,
    DateOnly? End,
    ReportDateCriterion? Criterion,
    Guid? CategoryId,
    ManualTransactionStatus? Status,
    bool InvalidMonth,
    bool InvalidPeriod,
    bool InvalidCriterion,
    bool InvalidCategory,
    bool InvalidStatus)
{
    public static ReportQueryParameters Parse(Uri uri, DateOnly serverToday)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var values = QueryHelpers.ParseQuery(uri.Query);
        var defaultMonth = ReportPeriod.ForMonth(serverToday.Year, serverToday.Month);
        var month = defaultMonth.Start;
        var invalidMonth = false;
        if (values.TryGetValue("mes", out var monthValue))
        {
            invalidMonth = monthValue.Count != 1 || monthValue.ToString().Length != 7 ||
                !TryDate(monthValue + "-01", out month);
            if (invalidMonth)
            {
                month = defaultMonth.Start;
            }
        }

        ReportDateCriterion? criterion = null;
        var invalidCriterion = false;
        if (values.TryGetValue("criterio", out var criterionValue))
        {
            criterion = criterionValue.Count == 1 ? criterionValue.ToString() switch
            {
                "competencia" => ReportDateCriterion.Competence,
                "pagamento" => ReportDateCriterion.PaymentReceipt,
                _ => null
            } : null;
            invalidCriterion = criterion is null;
        }

        DateOnly? start = defaultMonth.Start;
        DateOnly? end = defaultMonth.End;
        var hasStart = values.TryGetValue("inicio", out var startValue);
        var hasEnd = values.TryGetValue("fim", out var endValue);
        var invalidPeriod = false;
        if (hasStart || hasEnd)
        {
            start = hasStart && TrySingleDate(startValue, out var parsedStart) ? parsedStart : null;
            end = hasEnd && TrySingleDate(endValue, out var parsedEnd) ? parsedEnd : null;
            try
            {
                _ = ReportPeriod.ForRange(start, end);
            }
            catch (ArgumentException)
            {
                invalidPeriod = true;
            }
        }

        Guid? categoryId = null;
        var invalidCategory = false;
        if (values.TryGetValue("categoria", out var categoryValue))
        {
            if (categoryValue.Count == 1 && Guid.TryParse(categoryValue.ToString(), out var parsedCategory) && parsedCategory != Guid.Empty)
            {
                categoryId = parsedCategory;
            }
            else
            {
                invalidCategory = true;
            }
        }

        ManualTransactionStatus? status = null;
        var invalidStatus = false;
        if (values.TryGetValue("estado", out var statusValue))
        {
            status = statusValue.Count == 1 ? statusValue.ToString() switch
            {
                "previsto" => ManualTransactionStatus.Planned,
                "realizado" => ManualTransactionStatus.Realized,
                _ => null
            } : null;
            invalidStatus = status is null;
        }

        return new ReportQueryParameters(month, start, end, criterion, categoryId, status,
            invalidMonth, invalidPeriod, invalidCriterion, invalidCategory, invalidStatus);
    }

    private static bool TrySingleDate(StringValues value, out DateOnly date) =>
        value.Count == 1 && TryDate(value.ToString(), out date) || SetDefault(out date);

    private static bool TryDate(string value, out DateOnly date) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static bool SetDefault(out DateOnly date)
    {
        date = default;
        return false;
    }
}