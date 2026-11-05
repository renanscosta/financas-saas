using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Domain.Tests.Finance;

public sealed class ReportDateRuleTests
{
    private static readonly DateOnly CompetenceDate = new(2026, 8, 15);
    private static readonly DateOnly ExpectedDate = new(2026, 9, 20);
    private static readonly DateOnly EffectiveDate = new(2026, 10, 25);

    [Theory]
    [InlineData(ReportDateCriterion.Competence, ManualTransactionStatus.Planned, 8)]
    [InlineData(ReportDateCriterion.Competence, ManualTransactionStatus.Realized, 8)]
    [InlineData(ReportDateCriterion.PaymentReceipt, ManualTransactionStatus.Planned, 9)]
    [InlineData(ReportDateCriterion.PaymentReceipt, ManualTransactionStatus.Realized, 10)]
    public void DataDeReferenciaSegueCriterioEEstado(
        ReportDateCriterion criterion,
        ManualTransactionStatus status,
        int expectedMonth)
    {
        var result = ReportDateRule.GetReferenceDate(criterion, status, CompetenceDate, ExpectedDate, EffectiveDate);

        Assert.Equal(expectedMonth, result!.Value.Month);
    }

    [Theory]
    [InlineData(ReportDateCriterion.Competence)]
    [InlineData(ReportDateCriterion.PaymentReceipt)]
    public void CanceladosNaoPossuemDataDeReferencia(ReportDateCriterion criterion)
    {
        var result = ReportDateRule.GetReferenceDate(
            criterion, ManualTransactionStatus.Cancelled, CompetenceDate, ExpectedDate, EffectiveDate);

        Assert.Null(result);
    }

    [Fact]
    public void RealizadoSemDataEfetivaFalhaExplicitamente()
    {
        Assert.Throws<ArgumentException>(() => ReportDateRule.GetReferenceDate(
            ReportDateCriterion.PaymentReceipt, ManualTransactionStatus.Realized, CompetenceDate, ExpectedDate, null));
    }

    [Fact]
    public void CriterioDesconhecidoFalhaExplicitamente()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReportDateRule.GetReferenceDate(
            (ReportDateCriterion)99, ManualTransactionStatus.Planned, CompetenceDate, ExpectedDate, null));
    }

    [Fact]
    public void EstadoDesconhecidoFalhaExplicitamente()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ReportDateRule.GetReferenceDate(
            ReportDateCriterion.Competence, (ManualTransactionStatus)99, CompetenceDate, ExpectedDate, null));
    }
}