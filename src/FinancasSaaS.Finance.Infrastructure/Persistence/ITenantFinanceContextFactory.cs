namespace FinancasSaaS.Finance.Infrastructure.Persistence;

public interface ITenantFinanceContextFactory
{
    Task<TenantDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}
