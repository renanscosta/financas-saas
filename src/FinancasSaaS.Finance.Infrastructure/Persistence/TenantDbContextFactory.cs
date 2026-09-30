using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FinancasSaaS.Finance.Infrastructure.Persistence;

public sealed class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=financas_tenant_design;Username=financas;Password=local-only-change-me")
            .Options;

        return new TenantDbContext(options);
    }
}
