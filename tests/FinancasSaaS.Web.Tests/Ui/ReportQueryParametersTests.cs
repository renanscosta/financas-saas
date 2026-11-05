using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Web.Components;

namespace FinancasSaaS.Web.Tests.Ui;

public sealed class ReportQueryParametersTests
{
    private static readonly DateOnly Today = new(2026, 10, 31);

    [Fact]
    public void AusenciaDeParametrosUsaMesDoServidorSemEscolherCriterio()
    {
        var result = ReportQueryParameters.Parse(new Uri("https://localhost/cliente/relatorios"), Today);

        Assert.Equal(new DateOnly(2026, 10, 1), result.Month);
        Assert.Equal(new DateOnly(2026, 10, 1), result.Start);
        Assert.Equal(new DateOnly(2026, 10, 31), result.End);
        Assert.Null(result.Criterion);
        Assert.False(result.InvalidCriterion);
    }

    [Fact]
    public void ParametrosValidosSaoLidosSemAceitarTenant()
    {
        var category = Guid.NewGuid();
        var result = ReportQueryParameters.Parse(new Uri($"https://localhost/cliente/relatorios?mes=2025-02&criterio=pagamento&inicio=2025-02-01&fim=2025-02-28&categoria={category}&estado=realizado&tenant=outro"), Today);

        Assert.Equal(new DateOnly(2025, 2, 1), result.Month);
        Assert.Equal(ReportDateCriterion.PaymentReceipt, result.Criterion);
        Assert.Equal(category, result.CategoryId);
        Assert.Equal(ManualTransactionStatus.Realized, result.Status);
        Assert.False(result.InvalidPeriod);
        Assert.False(result.InvalidCategory);
    }

    [Fact]
    public void ParametrosInvalidosSaoIndicadosSemSubstituirPeriodoOuCategoria()
    {
        var result = ReportQueryParameters.Parse(new Uri("https://localhost/cliente/relatorios?mes=nao&criterio=nao&inicio=2026-03-10&fim=2026-02-01&categoria=nao&estado=nao"), Today);

        Assert.True(result.InvalidMonth);
        Assert.Equal(new DateOnly(2026, 10, 1), result.Month);
        Assert.True(result.InvalidCriterion);
        Assert.Null(result.Criterion);
        Assert.True(result.InvalidPeriod);
        Assert.True(result.InvalidCategory);
        Assert.True(result.InvalidStatus);
    }

    [Fact]
    public void DataAusenteEmPeriodoInformadoNaoUsaPadraoSilencioso()
    {
        var result = ReportQueryParameters.Parse(new Uri("https://localhost/cliente/relatorios?inicio=2026-01-01"), Today);

        Assert.True(result.InvalidPeriod);
        Assert.Null(result.End);
    }
}