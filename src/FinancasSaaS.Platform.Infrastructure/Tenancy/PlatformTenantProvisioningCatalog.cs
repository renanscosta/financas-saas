using FinancasSaaS.Application.Abstractions;
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

        return tenant is not null && options.Value.Connections.TryGetValue(tenant.DatabaseName, out var connection)
            ? new TenantProvisioningTarget(tenant.DatabaseName, connection)
            : null;
    }
}