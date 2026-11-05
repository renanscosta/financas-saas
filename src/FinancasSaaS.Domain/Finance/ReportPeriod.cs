namespace FinancasSaaS.Domain.Finance;

public sealed class ReportPeriod
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    private ReportPeriod(DateOnly start, DateOnly end)
    {
        Start = start;
        End = end;
    }

    public static ReportPeriod ForMonth(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        return new ReportPeriod(start, new DateOnly(year, month, DateTime.DaysInMonth(year, month)));
    }

    public static ReportPeriod ForRange(DateOnly? start, DateOnly? end)
    {
        if (start is null || end is null)
        {
            throw new ArgumentException("As datas inicial e final são obrigatórias.");
        }

        if (start > end)
        {
            throw new ArgumentException("A data inicial não pode ser posterior à data final.");
        }

        if (MonthIndex(end.Value) - MonthIndex(start.Value) >= 24)
        {
            throw new ArgumentOutOfRangeException(nameof(end), "O intervalo não pode exceder 24 meses de calendário.");
        }

        return new ReportPeriod(start.Value, end.Value);
    }

    public bool Contains(DateOnly date, ReportDateCriterion criterion) => criterion switch
    {
        ReportDateCriterion.Competence => MonthIndex(date) >= MonthIndex(Start) && MonthIndex(date) <= MonthIndex(End),
        ReportDateCriterion.PaymentReceipt => date >= Start && date <= End,
        _ => throw new ArgumentOutOfRangeException(nameof(criterion))
    };

    private static int MonthIndex(DateOnly date) => (date.Year * 12) + date.Month;
}