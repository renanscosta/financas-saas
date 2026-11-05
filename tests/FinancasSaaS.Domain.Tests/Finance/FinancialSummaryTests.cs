using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Domain.Tests.Finance;

public sealed class FinancialSummaryTests
{
    [Fact]
    public void SaldosUsamTotaisDoProprioEstadoComCentavosExatos()
    {
        var summary = new FinancialSummary(100.01m, 60.05m, 140.02m, 10.01m);

        Assert.Equal(-40.01m, summary.PlannedBalance);
        Assert.Equal(50.04m, summary.RealizedBalance);
    }

    [Fact]
    public void SemPrevistosSaldoPrevistoEZeradoMesmoComRealizados()
    {
        var summary = new FinancialSummary(0m, 200m, 0m, 300m);

        Assert.Equal(0m, summary.PlannedBalance);
        Assert.Equal(-100m, summary.RealizedBalance);
    }

    [Fact]
    public void EntradasEDespesasIguaisResultamEmSaldosZerados()
    {
        var summary = new FinancialSummary(50.25m, 12.30m, 50.25m, 12.30m);

        Assert.Equal(0m, summary.PlannedBalance);
        Assert.Equal(0m, summary.RealizedBalance);
    }
}