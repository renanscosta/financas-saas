using Microsoft.AspNetCore.Mvc.Testing;

namespace FinancasSaaS.Web.Tests.Finance;

public sealed class ManualTransactionsAuthorizationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> factory = new();

    [Theory]
    [InlineData("/cliente/lancamentos")]
    [InlineData("/cliente/lancamentos?transactionId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")]
    public async Task VisitanteNaoAcessaLancamentosNemIdsManipulados(string path)
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
