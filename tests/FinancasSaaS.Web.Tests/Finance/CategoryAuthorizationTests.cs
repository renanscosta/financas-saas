using Microsoft.AspNetCore.Mvc.Testing;

namespace FinancasSaaS.Web.Tests.Finance;

public sealed class CategoryAuthorizationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> factory = new();

    [Theory]
    [InlineData("/cliente/categorias")]
    [InlineData("/cliente/categorias?categoryId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    public async Task VisitanteNaoAcessaCategoriasNemIdsManipulados(string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(path);

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => factory.Dispose();
}
