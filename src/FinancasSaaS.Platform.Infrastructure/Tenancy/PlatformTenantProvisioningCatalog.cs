using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Domain.Platform;
using FinancasSaaS.Platform.Infrastructure.Configuration;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FinancasSaaS.Platform.Infrastructure.Tenancy;

public sealed class PlatformTenantProvisioningCatalog(
    PlatformDbContext dbContext,
    IOptions<TenantDatabaseOptions> options) : ITenantProvisioningCatalog
{
    public async Task<TenantProvisioningTarget?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await dbContext.Tenants
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == tenantId, cancellationToken);

        return tenant is not null && options.Value.GetConnectionString(tenant.DatabaseName) is { } connection
            ? new TenantProvisioningTarget(tenant.DatabaseName, connection)
            : null;
    }

    public async Task<IReadOnlyList<TenantFinanceMigrationTarget>> ListReadyAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await dbContext.Tenants
            .AsNoTracking()
            .Where(item => item.ProvisioningStatus == ProvisioningStatus.Ready)
            .OrderBy(item => item.Id)
            .Select(item => new { item.Id, item.DatabaseName })
            .ToListAsync(cancellationToken);

        return tenants
            .Select(item => new TenantFinanceMigrationTarget(
                item.Id,
                item.DatabaseName,
                options.Value.GetConnectionString(item.DatabaseName)))
            .ToArray();
    }
}