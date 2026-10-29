using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using FinancasSaaS.Web.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FinancasSaaS.Web.Tests.Authentication;

public sealed class AuthenticationEndToEndTests : IAsyncLifetime
{
    private readonly string connection =
        Environment.GetEnvironmentVariable("PLATFORM_CONNECTION_STRING")
        ?? throw new InvalidOperationException("Defina PLATFORM_CONNECTION_STRING para os testes de autenticação Web.");
    private WebApplicationFactory<Program> factory = null!;
    private string administratorEmail = string.Empty;
    private string readonlyEmail = string.Empty;

    public async Task InitializeAsync()
    {
        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Platform", connection));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await db.Database.MigrateAsync();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureRoleAsync(roleManager, PlatformRoles.Administrator);
        await EnsureRoleAsync(roleManager, PlatformRoles.ReadOnlyOperator);

        administratorEmail = $"e2e-admin-{Guid.NewGuid():N}@example.com";
        readonlyEmail = $"e2e-readonly-{Guid.NewGuid():N}@example.com";
        await CreateUserAsync(userManager, administratorEmail, PlatformRoles.Administrator);
        await CreateUserAsync(userManager, readonlyEmail, PlatformRoles.ReadOnlyOperator);
    }

    [Fact]
    public async Task LoginAdministraCookieEEfetivaPolicyDeMutacao()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var login = await LoginAsync(client, administratorEmail);

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.NotNull(login.Headers.Location);
        Assert.DoesNotContain("Account/Login", login.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);

        var protectedPage = await client.GetAsync("/platform/customers/register");
        Assert.Equal(HttpStatusCode.OK, protectedPage.StatusCode);
    }

    [Fact]
    public async Task LoginSomenteLeituraRecebeCookieMasNaoAcessaMutacao()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var login = await LoginAsync(client, readonlyEmail);

        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        var protectedPage = await client.GetAsync("/platform/customers/register");
        Assert.Equal(HttpStatusCode.Redirect, protectedPage.StatusCode);
        Assert.Contains("Account/AccessDenied", protectedPage.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NavegacaoDaPlataformaRespeitaPermissoesDoOperador()
    {
        using var administratorClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(administratorClient, administratorEmail);

        var administratorHome = await administratorClient.GetStringAsync("/");
        Assert.Contains("href=\"platform/customers/register\"", administratorHome, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"platform/customers/deactivate\"", administratorHome, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("href=\"platform/customers/invite\"", administratorHome, StringComparison.OrdinalIgnoreCase);

        using var readonlyClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(readonlyClient, readonlyEmail);

        var readonlyHome = await readonlyClient.GetStringAsync("/");
        Assert.Contains("href=\"platform/customers/provisioning\"", readonlyHome, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"platform/customers/register\"", readonlyHome, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"platform/customers/deactivate\"", readonlyHome, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("href=\"platform/customers/invite\"", readonlyHome, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LogoutPostComAntifalsificacaoEncerraSessao()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await LoginAsync(client, administratorEmail);

        var home = await client.GetStringAsync("/");
        Assert.Contains("action=\"/Account/Logout\"", home, StringComparison.OrdinalIgnoreCase);
        var token = Regex.Match(home, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value;
        Assert.False(string.IsNullOrWhiteSpace(token));

        var logout = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        ]));

        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        var protectedPage = await client.GetAsync("/platform/customers/register");
        Assert.Equal(HttpStatusCode.Redirect, protectedPage.StatusCode);
        Assert.Contains("Account/Login", protectedPage.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VisitanteNaoAcessaRotaAdministrativaDiretamente()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var protectedPage = await client.GetAsync("/platform/customers/register");

        Assert.Equal(HttpStatusCode.Redirect, protectedPage.StatusCode);
        Assert.Contains("Account/Login", protectedPage.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginApresentaRotulosProgramaticosParaCampos()
    {
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/Account/Login");

        Assert.Contains("<label for=\"email\">E-mail</label>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"Input.Email\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<label for=\"password\">Senha</label>", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("name=\"Input.Password\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<label for=\"remember-me\">Lembrar de mim</label>", html, StringComparison.OrdinalIgnoreCase);
    }

    public Task DisposeAsync()
    {
        factory.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email)
    {
        var loginPage = await client.GetStringAsync("/Account/Login");
        var token = Regex.Match(loginPage, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        Assert.False(string.IsNullOrWhiteSpace(token));

        return await client.PostAsync("/Account/Login", new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("Input.Email", email),
            new KeyValuePair<string, string>("Input.Password", "StrongPassword123!"),
            new KeyValuePair<string, string>("Input.RememberMe", "false"),
            new KeyValuePair<string, string>("__RequestVerificationToken", token)
        ]));
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole<Guid>> roleManager, string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
        }
    }

    private static async Task CreateUserAsync(UserManager<ApplicationUser> userManager, string email, string role)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            AccessEnabled = true
        };
        var result = await userManager.CreateAsync(user, "StrongPassword123!");
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
        result = await userManager.AddToRoleAsync(user, role);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
    }
}