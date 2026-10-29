using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Domain.Tests.Finance;

public sealed class CategoryTests
{
    [Fact]
    public void CategoriaRemoveEspacosDasExtremidades()
    {
        var category = Category.Create(Guid.NewGuid(), "  Pessoal  ");

        Assert.Equal("Pessoal", category.Name);
    }

    [Fact]
    public void NomeNormalizadoIgnoraCaixaEEspacosDasExtremidades()
    {
        Assert.Equal(Category.NormalizeName(" Lanches "), Category.NormalizeName("lAnChEs"));
    }

    [Fact]
    public void NomeVazioOuMaiorQueLimiteERecusado()
    {
        Assert.Throws<ArgumentException>(() => Category.Create(Guid.NewGuid(), "  "));
        Assert.Throws<ArgumentException>(() => Category.Create(Guid.NewGuid(), new string('a', 121)));
    }

    [Fact]
    public void RenomearValidaONovoNome()
    {
        var category = Category.Create(Guid.NewGuid(), "Mercado");

        category.Rename("  Pessoal ");

        Assert.Equal("Pessoal", category.Name);
    }
}
