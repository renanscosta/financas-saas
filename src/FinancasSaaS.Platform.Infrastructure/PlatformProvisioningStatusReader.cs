using FinancasSaaS.Application.Platform.GetProvisioningStatus;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Platform.Infrastructure;

public sealed class PlatformProvisioningStatusReader(PlatformDbContext dbContext) : IProvisioningStatusReader
{
    public async Task<ProvisioningStatusResult?> ReadAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Provisionings
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .Select(item => new ProvisioningStatusResult(
                item.TenantId,
                item.Status.ToString(),
                item.Attempt,
                item.LastFailure,
                item.LastAttemptAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
