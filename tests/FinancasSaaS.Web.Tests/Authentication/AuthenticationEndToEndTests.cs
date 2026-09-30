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
    private readonly string connection = "Host=localhost;Port=5432;Database=financas_platform;Username=financas;Password=local-only-change-me";
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