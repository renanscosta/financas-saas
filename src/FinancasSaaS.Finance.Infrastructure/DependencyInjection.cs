using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Finance.Infrastructure.Categories;
using FinancasSaaS.Finance.Infrastructure.InstallmentPurchases;
using FinancasSaaS.Finance.Infrastructure.ManualTransactions;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Finance.Infrastructure.Provisioning;
using FinancasSaaS.Finance.Infrastructure.Reports;
using Microsoft.Extensions.DependencyInjection;

namespace FinancasSaaS.Finance.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFinanceInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ITenantProvisioner, PostgresTenantProvisioner>();
        services.AddScoped<ITenantFinanceContextFactory, TenantFinanceContextFactory>();
        services.AddScoped<TenantFinanceMigrationRunner>();
        services.AddScoped<ICategoryStore, CategoryStore>();
        services.AddScoped<IManualTransactionStore, ManualTransactionStore>();
        services.AddScoped<IInstallmentPurchaseStore, InstallmentPurchaseStore>();
        services.AddScoped<IFinanceReportStore, FinanceReportStore>();
        return services;
    }
}
