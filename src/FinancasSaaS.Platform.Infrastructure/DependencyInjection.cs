using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Platform.RegisterCustomer;
using FinancasSaaS.Application.Platform.GetProvisioningStatus;
using FinancasSaaS.Application.Platform.Invitations;
using FinancasSaaS.Application.Platform.CompleteRegistration;
using FinancasSaaS.Application.Platform.DeactivateCustomer;
using FinancasSaaS.Platform.Infrastructure.Auditing;
using FinancasSaaS.Platform.Infrastructure.Configuration;
using FinancasSaaS.Platform.Infrastructure.Email;
using FinancasSaaS.Platform.Infrastructure.Identity;
using FinancasSaaS.Platform.Infrastructure.Tenancy;
using FinancasSaaS.Platform.Infrastructure.Services;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace FinancasSaaS.Platform.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPlatformInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenantDatabaseOptions>(configuration.GetSection("TenantDatabases"));
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.Configure<IdentitySeedOptions>(configuration.GetSection("IdentitySeed"));
        services.AddScoped<ITenantConnectionResolver, PlatformTenantConnectionResolver>();
        services.AddScoped<ITenantProvisioningCatalog, PlatformTenantProvisioningCatalog>();
        services.AddScoped<ITenantProvisioningStateStore>(provider =>
            new PlatformTenantProvisioningStateStore(
                provider.GetRequiredService<PlatformDbContext>(),
                provider.GetRequiredService<IAuditWriter>()));
        services.AddScoped<ICustomerRegistrationStore, PlatformCustomerRegistrationStore>();
        services.AddScoped<IProvisioningStatusReader, PlatformProvisioningStatusReader>();
        services.AddScoped<IInvitationStore, PlatformInvitationStore>();
        services.AddScoped<IRegistrationCompletionStore, PlatformRegistrationCompletionStore>();
        services.AddScoped<ICustomerDeactivationStore, PlatformCustomerDeactivationStore>();
        services.AddScoped<PlatformIdentitySeeder>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IAuditWriter, PlatformAuditWriter>();
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<ITokenHasher, Sha256TokenHasher>();
        services.AddSingleton<ITokenGenerator, RandomTokenGenerator>();
        return services;
    }
}
