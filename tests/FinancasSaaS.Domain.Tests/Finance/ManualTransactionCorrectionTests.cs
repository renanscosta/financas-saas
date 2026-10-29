using FinancasSaaS.Domain.Finance;
using System.Globalization;

namespace FinancasSaaS.Domain.Tests.Finance;

public sealed class ManualTransactionCorrectionTests
{
    [Fact]
    public void CorrecaoDeRealizadoPreservaEstadoEDataEfetivaERegistraAuditoria()
    {
        var transaction = Create();
        var effectiveDate = new DateOnly(2026, 10, 29);
        transaction.Realize(effectiveDate);
        var changedCategory = Guid.NewGuid();

        var audit = transaction.Correct(
            new ManualTransactionCorrection(
                null,
                "Almoço corrigido",
                15.50m,
                changedCategory,
                null,
                null,
                "Corrigir lançamento"),
            DateTimeOffset.Parse("2026-10-27T18:00:00Z", CultureInfo.InvariantCulture),
            Guid.NewGuid());

        Assert.Equal(ManualTransactionStatus.Realized, transaction.Status);
        Assert.Equal(effectiveDate, transaction.EffectiveDate);
        Assert.Equal("Corrigir lançamento", audit.Reason);
        Assert.Contains(audit.Changes, change => change.Field == "Amount" && change.PreviousValue == "10.00" && change.NewValue == "15.50");
    }

    [Fact]
    public void CorrecaoExigeMotivoENaoAlteraLancamentoEmFalha()
    {
        var transaction = Create();

        Assert.Throws<ArgumentException>(() => transaction.Correct(
            new ManualTransactionCorrection(null, null, 15m, null, null, null, " "),
            DateTimeOffset.Parse("2026-10-27T18:00:00Z", CultureInfo.InvariantCulture),
            Guid.NewGuid()));

        Assert.Equal(10m, transaction.Amount);
    }

    [Fact]
    public void CorrecaoRecusaValorComMaisDeDuasCasasSemArredondar()
    {
        var transaction = Create();

        Assert.Throws<ArgumentOutOfRangeException>(() => transaction.Correct(
            new ManualTransactionCorrection(null, null, 10.001m, null, null, null, "Corrigir"),
            DateTimeOffset.Parse("2026-10-27T18:00:00Z", CultureInfo.InvariantCulture),
            Guid.NewGuid()));

        Assert.Equal(10m, transaction.Amount);
    }

    [Fact]
    public void CorrecaoAceitaValorMaximoSuportadoPeloBanco()
    {
        var transaction = Create();

        transaction.Correct(
            new ManualTransactionCorrection(null, null, 9999999999999999.99m, null, null, null, "Corrigir"),
            DateTimeOffset.Parse("2026-10-27T18:00:00Z", CultureInfo.InvariantCulture),
            Guid.NewGuid());

        Assert.Equal(9999999999999999.99m, transaction.Amount);
    }

    [Fact]
    public void CorrecaoRecusaValorAcimaDoMaximoSemAlterarLancamento()
    {
        var transaction = Create();

        Assert.Throws<ArgumentOutOfRangeException>(() => transaction.Correct(
            new ManualTransactionCorrection(null, null, 10000000000000000.00m, null, null, null, "Corrigir"),
            DateTimeOffset.Parse("2026-10-27T18:00:00Z", CultureInfo.InvariantCulture),
            Guid.NewGuid()));

        Assert.Equal(10m, transaction.Amount);
    }

    [Fact]
    public void CorrecaoSemCamposAlteradosERecusada()
    {
        var transaction = Create();

        Assert.Throws<ArgumentException>(() => transaction.Correct(
            new ManualTransactionCorrection(null, null, null, null, null, null, "Corrigir"),
            DateTimeOffset.Parse("2026-10-27T18:00:00Z", CultureInfo.InvariantCulture),
            Guid.NewGuid()));
    }

    private static ManualTransaction Create() =>
        ManualTransaction.Create(
            Guid.NewGuid(),
            ManualTransactionType.Expense,
            "Almoço",
            10m,
            Guid.NewGuid(),
            new DateOnly(2026, 10, 27),
            new DateOnly(2026, 10, 28));
}
