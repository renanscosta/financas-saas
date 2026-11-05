using Microsoft.AspNetCore.Mvc.Testing;

namespace FinancasSaaS.Web.Tests.Finance;

public sealed class FinanceReportDashboardAuthorizationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> factory = new();

    [Theory]
    [InlineData("/cliente/dashboard")]
    [InlineData("/cliente/dashboard?tenant=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&mes=invalido")]
    [InlineData("/cliente/relatorios")]
    [InlineData("/cliente/relatorios?tenant=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa&categoria=invalida")]
    public async Task VisitanteNaoAcessaDashboard(string path)
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync(path);

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose() => factory.Dispose();
}