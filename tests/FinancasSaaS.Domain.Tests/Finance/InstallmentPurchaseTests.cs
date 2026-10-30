using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Domain.Tests.Finance;

public sealed class InstallmentPurchaseTests
{
    [Fact]
    public void CriaParcelasNumeradasComSomaExataEAuditoriaInicialCompleta()
    {
        var purchase = Create(100m, 3, new DateOnly(2026, 1, 31));
        var audit = purchase.CreateInitialAuditEvent(Utc(2026, 1, 1), Guid.NewGuid());

        Assert.Equal([33.34m, 33.33m, 33.33m], purchase.Installments.Select(item => item.Amount));
        Assert.Equal([1, 2, 3], purchase.Installments.Select(item => item.Number));
        Assert.Equal(100m, purchase.Installments.Sum(item => item.Amount));
        Assert.Equal("Creation", audit.EventType);
        Assert.Contains(audit.Changes, item => item.InstallmentNumber == 1 && item.Field == "Amount" && item.NewValue == "33.34");
        Assert.Equal(3, audit.Changes.Where(item => item.Field == "DueDate").Count());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(1201)]
    public void RecusaQuantidadeForaDoIntervaloPermitido(int count)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create(100m, count, new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void RecusaDivisaoQueProduziriaParcelaSemValorPositivo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create(1m, 1200, new DateOnly(2026, 1, 1)));
    }

    [Fact]
    public void RealizaSomenteParcelaSelecionadaERecusaDuplicidade()
    {
        var purchase = Create(60m, 3, new DateOnly(2026, 1, 31));
        var effectiveDate = new DateOnly(2026, 1, 29);
        var audit = purchase.Realize(2, effectiveDate, Utc(2026, 1, 29), Guid.NewGuid());

        Assert.Equal(ManualTransactionStatus.Planned, purchase.Installments[0].Status);
        Assert.Equal(ManualTransactionStatus.Realized, purchase.Installments[1].Status);
        Assert.Equal(effectiveDate, purchase.Installments[1].EffectiveDate);
        Assert.Equal(ManualTransactionStatus.Planned, purchase.Installments[2].Status);
        Assert.Equal("Realization", audit.EventType);
        Assert.Equal(2, audit.Changes[0].InstallmentNumber);
        Assert.Throws<InvalidOperationException>(() =>
            purchase.Realize(2, effectiveDate, Utc(2026, 1, 29), Guid.NewGuid()));
    }

    [Fact]
    public void CriacaoRecusaMaisDeDuasCasasDecimais()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Create(10.001m, 2, new DateOnly(2026, 1, 1)));
    }

    private static InstallmentPurchase Create(decimal total, int count, DateOnly firstDueDate) =>
        InstallmentPurchase.Create(
            Guid.NewGuid(),
            "Compra de teste",
            Guid.NewGuid(),
            total,
            count,
            firstDueDate,
            Utc(2026, 1, 1));

    private static DateTimeOffset Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);
}
