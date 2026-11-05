using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Domain.Tests.Finance;

public sealed class ReportPeriodTests
{
    [Fact]
    public void PeriodoMensalIncluiTodosOsDiasDoMes()
    {
        var period = ReportPeriod.ForMonth(2028, 2);

        Assert.Equal(new DateOnly(2028, 2, 1), period.Start);
        Assert.Equal(new DateOnly(2028, 2, 29), period.End);
        Assert.True(period.Contains(new DateOnly(2028, 2, 29), ReportDateCriterion.PaymentReceipt));
        Assert.False(period.Contains(new DateOnly(2028, 3, 1), ReportDateCriterion.PaymentReceipt));
    }

    [Fact]
    public void IntervaloAceitaVinteEQuatroMesesERejeitaVinteECinco()
    {
        var start = new DateOnly(2025, 1, 1);

        _ = ReportPeriod.ForRange(start, new DateOnly(2026, 12, 31));
        Assert.Throws<ArgumentOutOfRangeException>(() => ReportPeriod.ForRange(start, new DateOnly(2027, 1, 1)));
    }

    [Fact]
    public void IntervaloRejeitaDatasAusentesOuInvertidas()
    {
        var date = new DateOnly(2026, 1, 10);

        Assert.Throws<ArgumentException>(() => ReportPeriod.ForRange(null, date));
        Assert.Throws<ArgumentException>(() => ReportPeriod.ForRange(date, null));
        Assert.Throws<ArgumentException>(() => ReportPeriod.ForRange(date, date.AddDays(-1)));
    }

    [Fact]
    public void PagamentoUsaDatasExatasInclusivas()
    {
        var period = ReportPeriod.ForRange(new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 10));

        Assert.False(period.Contains(new DateOnly(2026, 1, 14), ReportDateCriterion.PaymentReceipt));
        Assert.True(period.Contains(new DateOnly(2026, 1, 15), ReportDateCriterion.PaymentReceipt));
        Assert.True(period.Contains(new DateOnly(2026, 2, 10), ReportDateCriterion.PaymentReceipt));
        Assert.False(period.Contains(new DateOnly(2026, 2, 11), ReportDateCriterion.PaymentReceipt));
    }

    [Fact]
    public void CompetenciaIncluiOsMesesDeCalendarioInteiros()
    {
        var period = ReportPeriod.ForRange(new DateOnly(2026, 1, 15), new DateOnly(2026, 2, 10));

        Assert.True(period.Contains(new DateOnly(2026, 1, 1), ReportDateCriterion.Competence));
        Assert.True(period.Contains(new DateOnly(2026, 2, 28), ReportDateCriterion.Competence));
        Assert.False(period.Contains(new DateOnly(2026, 3, 1), ReportDateCriterion.Competence));
    }

    [Fact]
    public void UltimoMesSuportadoNaoExigeAnoSeguinte()
    {
        var period = ReportPeriod.ForMonth(9999, 12);

        Assert.True(period.Contains(DateOnly.MaxValue, ReportDateCriterion.Competence));
    }
}