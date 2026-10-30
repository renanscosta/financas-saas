using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Domain.Tests.Finance;

public sealed class InstallmentScheduleTests
{
    [Theory]
    [InlineData(2024, 29)]
    [InlineData(2025, 28)]
    public void VencimentoDia31AjustaFevereiroERetomaDiaOriginal(int year, int expectedFebruaryDay)
    {
        var start = new DateOnly(year, 1, 31);

        Assert.Equal(new DateOnly(year, 2, expectedFebruaryDay),
            InstallmentSchedule.GetDueDate(start, 2));
        Assert.Equal(new DateOnly(year, 3, 31), InstallmentSchedule.GetDueDate(start, 3));
    }

    [Theory]
    [InlineData(29, 2025, 28, 29)]
    [InlineData(30, 2025, 28, 30)]
    [InlineData(31, 2025, 28, 31)]
    public void VencimentosCurtosRetomamDiaOriginal(int startDay, int year, int februaryDay, int marchDay)
    {
        var start = new DateOnly(year, 1, startDay);

        Assert.Equal(new DateOnly(year, 2, februaryDay), InstallmentSchedule.GetDueDate(start, 2));
        Assert.Equal(new DateOnly(year, 3, marchDay), InstallmentSchedule.GetDueDate(start, 3));
    }

    [Fact]
    public void RejeitaNumeroInvalidoEDataForaDoIntervaloSuportado()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InstallmentSchedule.GetDueDate(new DateOnly(2026, 1, 1), 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            InstallmentSchedule.GetDueDate(new DateOnly(9999, 12, 1), 2));
    }
}
