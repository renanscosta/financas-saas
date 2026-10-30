namespace FinancasSaaS.Domain.Finance;

public static class InstallmentSchedule
{
    public static DateOnly GetDueDate(DateOnly firstDueDate, int installmentNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(installmentNumber, 1);

        var monthOffset = installmentNumber - 1;
        var targetMonth = new DateOnly(firstDueDate.Year, firstDueDate.Month, 1).AddMonths(monthOffset);
        var day = Math.Min(firstDueDate.Day, DateTime.DaysInMonth(targetMonth.Year, targetMonth.Month));
        return new DateOnly(targetMonth.Year, targetMonth.Month, day);
    }
}
