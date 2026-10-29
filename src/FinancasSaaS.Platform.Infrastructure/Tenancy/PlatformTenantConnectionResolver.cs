using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FinancasSaaS.Platform.Infrastructure.Tenancy;

public sealed class PlatformTenantConnectionResolver(
    PlatformDbContext dbContext,
    IOptions<TenantDatabaseOptions> options) : ITenantConnectionResolver
{
    public async Task<string?> ResolveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var target = await ResolveTargetAsync(userId, cancellationToken);
        return target is not null && options.Value.Connections.TryGetValue(target.Value.DatabaseName, out var connection)
            ? connection
            : null;
    }

    public async Task<Guid?> ResolveTenantIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var target = await ResolveTargetAsync(userId, cancellationToken);
        return target is not null && options.Value.Connections.ContainsKey(target.Value.DatabaseName)
            ? target.Value.TenantId
            : null;
    }

    private async Task<(Guid TenantId, string DatabaseName)?> ResolveTargetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user?.TenantId is not Guid tenantId || !user.AccessEnabled)
        {
            return null;
        }

        var databaseName = await dbContext.Tenants
            .AsNoTracking()
            .Join(
                dbContext.Customers.AsNoTracking(),
                tenant => tenant.CustomerId,
                customer => customer.Id,
                (tenant, customer) => new { tenant, customer })
            .Where(item =>
                item.tenant.Id == tenantId &&
                item.tenant.Status == Domain.Platform.CustomerStatus.Active &&
                item.customer.Status == Domain.Platform.CustomerStatus.Active &&
                item.tenant.ProvisioningStatus == Domain.Platform.ProvisioningStatus.Ready)
            .Select(item => item.tenant.DatabaseName)
            .SingleOrDefaultAsync(cancellationToken);

        return databaseName is null ? null : (tenantId, databaseName);
    }
}
