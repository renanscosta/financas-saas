using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Domain.Tests.Finance;

public sealed class ManualTransactionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CriacaoRecusaValorNaoPositivo(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(amount));
    }

    [Fact]
    public void CriacaoRecusaMaisDeDuasCasasSemArredondar()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(10.001m));
    }

    [Fact]
    public void CriacaoAceitaValorMaximoSuportadoPeloBanco()
    {
        var transaction = Create(9999999999999999.99m);

        Assert.Equal(9999999999999999.99m, transaction.Amount);
    }

    [Fact]
    public void CriacaoRecusaValorAcimaDoMaximoSuportadoPeloBanco()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(10000000000000000.00m));
    }

    [Fact]
    public void LancamentoNovoIniciaPrevistoERealizacaoRegistraDataEfetiva()
    {
        var transaction = Create(12.34m);
        var effectiveDate = new DateOnly(2026, 10, 28);

        Assert.Equal(ManualTransactionStatus.Planned, transaction.Status);
        transaction.Realize(effectiveDate);
        Assert.Equal(ManualTransactionStatus.Realized, transaction.Status);
        Assert.Equal(effectiveDate, transaction.EffectiveDate);
    }

    [Fact]
    public void RealizacaoSoPodeAcontecerUmaVez()
    {
        var transaction = Create(12m);
        transaction.Realize(new DateOnly(2026, 10, 28));

        Assert.Throws<InvalidOperationException>(() => transaction.Realize(new DateOnly(2026, 10, 29)));
    }

    [Fact]
    public void DataDeFiltroSegueEstadoAtualOuAnteriorAoCancelamento()
    {
        var expectedDate = new DateOnly(2026, 10, 28);
        var effectiveDate = new DateOnly(2026, 10, 30);
        var planned = Create(12m);
        var realized = Create(12m);
        realized.Realize(effectiveDate);
        var cancelledPlanned = Create(12m);
        cancelledPlanned.Cancel(DateTimeOffset.UtcNow, Guid.NewGuid(), "Registro incorreto.");
        var cancelledRealized = Create(12m);
        cancelledRealized.Realize(effectiveDate);
        cancelledRealized.Cancel(DateTimeOffset.UtcNow, Guid.NewGuid(), "Registro incorreto.");

        Assert.Equal(expectedDate, planned.FilterDate);
        Assert.Equal(effectiveDate, realized.FilterDate);
        Assert.Equal(expectedDate, cancelledPlanned.FilterDate);
        Assert.Equal(effectiveDate, cancelledRealized.FilterDate);
    }

    private static ManualTransaction Create(decimal amount) =>
        ManualTransaction.Create(
            Guid.NewGuid(),
            ManualTransactionType.Expense,
            "Mercado",
            amount,
            Guid.NewGuid(),
            new DateOnly(2026, 10, 27),
            new DateOnly(2026, 10, 28));
}
