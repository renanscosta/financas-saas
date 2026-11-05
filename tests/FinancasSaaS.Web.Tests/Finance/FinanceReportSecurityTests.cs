using System.Security.Claims;
using System.Text.Encodings.Web;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance.Reports;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancasSaaS.Web.Tests.Finance;

public sealed class FinanceReportSecurityTests : IDisposable
{
    private static readonly Guid AuthenticatedUserId = Guid.Parse("108ec19c-c981-4efb-9701-ecb7ee278c5d");
    private static readonly Guid AuthenticatedTenantId = Guid.Parse("119fd51e-3c88-41b6-9bd5-1f5a13c6a9c2");
    private readonly TestTenantConnectionResolver resolver = new();
    private readonly WebApplicationFactory<Program> factory;

    public FinanceReportSecurityTests()
    {
        factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    options.DefaultForbidScheme = TestAuthenticationHandler.SchemeName;
                }).AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(TestAuthenticationHandler.SchemeName, _ => { });
                services.RemoveAll<ICurrentUserContext>();
                services.AddSingleton<ICurrentUserContext>(new TestUser(AuthenticatedUserId, AuthenticatedTenantId));
                services.RemoveAll<ITenantConnectionResolver>();
                services.AddSingleton<ITenantConnectionResolver>(resolver);
                services.RemoveAll<IFinanceReportStore>();
                services.AddSingleton<IFinanceReportStore, EmptyReportStore>();
            }));
    }

    [Fact]
    public async Task ClienteAutenticadoNavegaNasRotasFinanceirasSemAceitarTenantDaUrl()
    {
        using var client = factory.CreateClient();

        var dashboard = await client.GetAsync($"/cliente/dashboard?tenant={Guid.NewGuid()}&valor=987654.32");
        var dashboardHtml = await dashboard.Content.ReadAsStringAsync();
        Assert.Equal(System.Net.HttpStatusCode.OK, dashboard.StatusCode);
        Assert.Contains("Dashboard financeiro", dashboardHtml, StringComparison.Ordinal);
        Assert.Equal(AuthenticatedUserId, resolver.LastResolvedUserId);
        Assert.DoesNotContain("987654.32", dashboardHtml, StringComparison.Ordinal);

        resolver.LastResolvedUserId = null;
        var reports = await client.GetAsync($"/cliente/relatorios?tenant={Guid.NewGuid()}&valor=987654.32");
        var reportsHtml = await reports.Content.ReadAsStringAsync();
        Assert.Equal(System.Net.HttpStatusCode.OK, reports.StatusCode);
        Assert.Contains("Relatórios financeiros", reportsHtml, StringComparison.Ordinal);
        Assert.Equal(AuthenticatedUserId, resolver.LastResolvedUserId);
        Assert.DoesNotContain("987654.32", reportsHtml, StringComparison.Ordinal);
    }

    public void Dispose() => factory.Dispose();

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "FinanceReportTest";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, AuthenticatedUserId.ToString()),
                new Claim(ClaimTypes.Name, "report-test-user"),
                new Claim("test-tenant-access", "active")
            };
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
        }
    }

    private sealed class TestTenantConnectionResolver : ITenantConnectionResolver
    {
        public Guid? LastResolvedUserId { get; set; }

        public Task<string?> ResolveAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            LastResolvedUserId = userId;
            return Task.FromResult<string?>(userId == AuthenticatedUserId ? "test-tenant-connection" : null);
        }

        public Task<Guid?> ResolveTenantIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(userId == AuthenticatedUserId ? AuthenticatedTenantId : null);
    }

    private sealed class TestUser(Guid userId, Guid tenantId) : ICurrentUserContext
    {
        public Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(userId);
        public Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(tenantId);
    }

    private sealed class EmptyReportStore : IFinanceReportStore
    {
        public Task<IReadOnlyList<FinanceReportAggregate>> GetDashboardAsync(FinanceReportQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FinanceReportAggregate>>([]);

        public Task<IReadOnlyList<FinanceReportAggregate>> GetExpensesAsync(FinanceReportQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FinanceReportAggregate>>([]);

        public Task<IReadOnlyList<FinanceReportCompositionItem>> GetCompositionAsync(FinanceReportQuery query, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FinanceReportCompositionItem>>([]);

        public Task<IReadOnlyList<FinanceReportCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FinanceReportCategory>>([]);

        public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<FinanceReportPreference> GetPreferenceAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FinanceReportPreference(ReportDateCriterion.Competence, true));

        public Task SavePreferenceAsync(Guid userId, ReportDateCriterion criterion, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
