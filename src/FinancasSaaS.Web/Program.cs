using FinancasSaaS.Web.Components;

using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using FinancasSaaS.Platform.Infrastructure.Identity;
using FinancasSaaS.Platform.Infrastructure;
using FinancasSaaS.Finance.Infrastructure;
using FinancasSaaS.Web.Infrastructure;
using FinancasSaaS.Web.Infrastructure.HealthChecks;
using FinancasSaaS.Web.Security;
using FinancasSaaS.Application.Platform.DeactivateCustomer;
using FinancasSaaS.Application.Platform.CompleteRegistration;
using FinancasSaaS.Application.Platform.GetProvisioningStatus;
using FinancasSaaS.Application.Platform.Invitations;
using FinancasSaaS.Application.Platform.ProvisionTenant;
using FinancasSaaS.Application.Platform.RegisterCustomer;
using FinancasSaaS.Application.Finance.Categories;
using FinancasSaaS.Application.Finance.ManualTransactions;
using FinancasSaaS.Application.Finance.InstallmentPurchases;
using FinancasSaaS.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using FinancasSaaS.Finance.Infrastructure.Provisioning;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var platformConnection = builder.Configuration.GetConnectionString("Platform");
if (string.IsNullOrWhiteSpace(platformConnection))
{
    platformConnection = Environment.GetEnvironmentVariable("PLATFORM_CONNECTION_STRING");
}

if (string.IsNullOrWhiteSpace(platformConnection))
{
    platformConnection = "Host=localhost;Port=5432;Database=financas_platform;Username=financas;Password=local-only-change-me";
}

builder.Services.AddDbContext<PlatformDbContext>(options => options.UseNpgsql(platformConnection));
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole<Guid>>()
    .AddSignInManager()
    .AddEntityFrameworkStores<PlatformDbContext>();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(PlatformPolicies.ReadProvisioning, policy => policy.RequireRole(PlatformRoles.ReadOnlyOperator, PlatformRoles.Administrator))
    .AddPolicy(PlatformPolicies.MutateProvisioning, policy => policy.RequireRole(PlatformRoles.Administrator))
    .AddPolicy(PlatformPolicies.TenantAccess, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new TenantAccessRequirement());
    });
builder.Services.AddScoped<IAuthorizationHandler, TenantAccessAuthorizationHandler>();
builder.Services.AddScoped<ICurrentUserContext, BlazorCurrentUserContext>();
builder.Services.AddScoped<ProvisioningRetryService>();
builder.Services.AddPlatformInfrastructure(builder.Configuration);
builder.Services.AddFinanceInfrastructure();
builder.Services.AddScoped<RegisterCustomerUseCase>();
builder.Services.AddScoped<GetProvisioningStatusUseCase>();
builder.Services.AddScoped<ProvisionTenantUseCase>();
builder.Services.AddScoped<IssueInvitationUseCase>();
builder.Services.AddScoped<SendInvitationUseCase>();
builder.Services.AddScoped<CompleteRegistrationUseCase>();
builder.Services.AddScoped<DeactivateCustomerUseCase>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ManualTransactionService>();
builder.Services.AddScoped<InstallmentPurchaseService>();
builder.Services.Configure<MailpitOptions>(builder.Configuration.GetSection("Mailpit"));
builder.Services.AddHealthChecks()
    .AddCheck<PlatformDatabaseHealthCheck>("postgresql")
    .AddCheck<MailpitHealthCheck>("mailpit");

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddRazorPages();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (FinanceMigrationCommand.IsCommand(args))
{
    using var commandScope = app.Services.CreateScope();
    var runner = commandScope.ServiceProvider.GetRequiredService<TenantFinanceMigrationRunner>();
    var command = new FinanceMigrationCommand(runner);
    Environment.ExitCode = await command.ExecuteAsync(args);
    return;
}

if (builder.Configuration.GetValue<bool>("IdentitySeed:Enabled"))
{
    using var identityScope = app.Services.CreateScope();
    await identityScope.ServiceProvider.GetRequiredService<PlatformIdentitySeeder>().SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
