using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace FinancasSaaS.Web.Tests.Onboarding;

public sealed class OnboardingEndToEndTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public OnboardingEndToEndTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task FormularioDeConviteNaoExibeNavegacaoAdministrativa()
    {
        var response = await client.GetAsync("/concluir-cadastro?token=test-token");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Concluir cadastro", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Senha", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Confirmação da senha", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("for=\"password\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("for=\"confirmation\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("platform/customers/register", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("platform/customers/deactivate", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("platform/customers/invite", html, StringComparison.OrdinalIgnoreCase);
    }
}
