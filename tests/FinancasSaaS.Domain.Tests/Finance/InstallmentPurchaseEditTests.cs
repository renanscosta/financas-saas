using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Domain.Tests.Finance;

public sealed class InstallmentPurchaseEditTests
{
    [Fact]
    public void EditaSaldoSomenteEntreParcelasFuturasEPreservaRealizada()
    {
        var purchase = Create(100m, 3);
        purchase.Realize(1, new DateOnly(2026, 1, 29), Utc(2026, 1, 29), Guid.NewGuid());

        var audit = purchase.Edit(
            "Compra atualizada",
            Guid.NewGuid(),
            110m,
            null,
            new DateOnly(2026, 1, 1),
            "Ajuste da compra",
            Utc(2026, 1, 30),
            Guid.NewGuid());

        Assert.Equal("Compra de teste", purchase.Installments[0].Description);
        Assert.Equal(33.34m, purchase.Installments[0].Amount);
        Assert.Equal(ManualTransactionStatus.Realized, purchase.Installments[0].Status);
        Assert.Equal(["Compra atualizada", "Compra atualizada"], purchase.Installments.Skip(1).Select(item => item.Description));
        Assert.Equal([38.33m, 38.33m], purchase.Installments.Skip(1).Select(item => item.Amount));
        Assert.Equal(110m, purchase.Installments.Sum(item => item.Amount));
        Assert.Equal("Edit", audit.EventType);
        Assert.Equal("Ajuste da compra", audit.Reason);
    }

    [Fact]
    public void ReprogramaParcelasComDiaDeReferenciaECompetenciaDoVencimento()
    {
        var purchase = Create(120m, 4);

        purchase.Edit(
            "Compra de teste",
            purchase.CategoryId,
            120m,
            new DateOnly(2026, 1, 30),
            new DateOnly(2025, 12, 1),
            "Reprogramar vencimentos",
            Utc(2025, 12, 1),
            Guid.NewGuid());

        Assert.Equal(
            [new DateOnly(2026, 1, 30), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 30), new DateOnly(2026, 4, 30)],
            purchase.Installments.Select(item => item.DueDate));
        Assert.All(purchase.Installments, item =>
        {
            Assert.Equal(item.DueDate.Year, item.CompetenceDate.Year);
            Assert.Equal(item.DueDate.Month, item.CompetenceDate.Month);
        });
    }

    [Fact]
    public void RecusaTotalInferiorAoMontantePreservadoSemAlterarEstado()
    {
        var purchase = Create(100m, 3);
        purchase.Realize(1, new DateOnly(2026, 1, 29), Utc(2026, 1, 29), Guid.NewGuid());
        var original = purchase.Installments.ToArray();

        Assert.Throws<InvalidOperationException>(() =>
            purchase.Edit(
                "Compra atualizada",
                Guid.NewGuid(),
                33.33m,
                null,
                new DateOnly(2026, 1, 1),
                "Total incorreto",
                Utc(2026, 1, 30),
                Guid.NewGuid()));

        Assert.Equal(original, purchase.Installments);
    }

    [Fact]
    public void RecusaAgendaQueCruzaVencimentoProtegidoSemAlteracaoParcial()
    {
        var purchase = Create(120m, 4);
        purchase.Realize(2, new DateOnly(2026, 2, 20), Utc(2026, 2, 20), Guid.NewGuid());
        var original = purchase.Installments.ToArray();

        Assert.Throws<InvalidOperationException>(() =>
            purchase.Edit(
                purchase.Description,
                purchase.CategoryId,
                purchase.TotalAmount,
                new DateOnly(2026, 3, 1),
                new DateOnly(2026, 1, 1),
                "Reprogramar",
                Utc(2026, 2, 21),
                Guid.NewGuid()));

        Assert.Equal(original, purchase.Installments);
    }

    [Fact]
    public void CancelamentoAfetaSomenteParcelasFuturasPrevistas()
    {
        var purchase = Create(90m, 3);
        purchase.Realize(1, new DateOnly(2026, 1, 30), Utc(2026, 1, 30), Guid.NewGuid());
        var audit = purchase.Cancel(
            new DateOnly(2026, 1, 1),
            "Compra cancelada",
            Utc(2026, 1, 31),
            Guid.NewGuid());

        Assert.Equal(ManualTransactionStatus.Realized, purchase.Installments[0].Status);
        Assert.All(purchase.Installments.Skip(1), item =>
        {
            Assert.Equal(ManualTransactionStatus.Cancelled, item.Status);
            Assert.Equal(ManualTransactionStatus.Planned, item.StatusBeforeCancellation);
        });
        Assert.Equal(2, audit.Changes.Count);
    }

    [Fact]
    public void PreservaParcelaVencidaERecusaOperacaoSemElegiveis()
    {
        var purchase = Create(60m, 2);
        var original = purchase.Installments.ToArray();

        Assert.Throws<InvalidOperationException>(() =>
            purchase.Cancel(new DateOnly(2026, 12, 31), "Sem futuras", Utc(2026, 12, 31), Guid.NewGuid()));

        Assert.Equal(original, purchase.Installments);
    }

    [Fact]
    public void RecusaMotivoAusente()
    {
        var purchase = Create(60m, 2);

        Assert.Throws<ArgumentException>(() =>
            purchase.Cancel(new DateOnly(2026, 1, 1), " ", Utc(2026, 1, 2), Guid.NewGuid()));
    }

    private static InstallmentPurchase Create(decimal total, int count) =>
        InstallmentPurchase.Create(
            Guid.NewGuid(),
            "Compra de teste",
            Guid.NewGuid(),
            total,
            count,
            new DateOnly(2026, 1, 31),
            Utc(2026, 1, 1));

    private static DateTimeOffset Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);
}
