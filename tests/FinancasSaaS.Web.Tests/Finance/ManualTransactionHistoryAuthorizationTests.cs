using Microsoft.AspNetCore.Mvc.Testing;

namespace FinancasSaaS.Web.Tests.Finance;

public sealed class ManualTransactionHistoryAuthorizationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> factory = new();

    [Fact]
    public async Task VisitanteNaoAcessaHistoricoPorIdDireto()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync(
            "/cliente/lancamentos?historyId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => factory.Dispose();
}
