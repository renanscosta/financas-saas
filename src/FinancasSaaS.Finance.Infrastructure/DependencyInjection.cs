using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Finance.Infrastructure.Provisioning;
using Microsoft.Extensions.DependencyInjection;

namespace FinancasSaaS.Finance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFinanceInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ITenantProvisioner, PostgresTenantProvisioner>();
        return services;
    }
}
