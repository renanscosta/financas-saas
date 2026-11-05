using FinancasSaaS.Web.Components;

namespace FinancasSaaS.Web.Tests.Ui;

public sealed class BrazilianFormatTests
{
    [Theory]
    [InlineData("1234.56", "R$ 1.234,56")]
    [InlineData("0", "R$ 0,00")]
    [InlineData("-200", "-R$ 200,00")]
    [InlineData("9999999999999999.99", "R$ 9.999.999.999.999.999,99")]
    public void ValoresUsamFormatoBrasileiro(string input, string expected)
    {
        var amount = decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(expected, BrazilianFormat.Money(amount));
    }

    [Fact]
    public void SaldoNegativoIncluiTextoSemDependerDeCor()
    {
        Assert.Equal("-R$ 200,00 (negativo)", BrazilianFormat.Balance(-200m));
        Assert.Equal("R$ 0,00", BrazilianFormat.Balance(0m));
    }

    [Fact]
    public void DatasEMesesUsamPortuguesBrasileiro()
    {
        var date = new DateOnly(2026, 10, 30);

        Assert.Equal("30/10/2026", BrazilianFormat.Date(date));
        Assert.Equal("outubro de 2026", BrazilianFormat.Month(date));
    }
}