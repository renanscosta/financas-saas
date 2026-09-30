using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;

namespace FinancasSaaS.Platform.Infrastructure.Auditing;

public sealed class PlatformAuditWriter(PlatformDbContext dbContext, ISystemClock clock) : IAuditWriter
{
    public async Task WriteAsync(string eventName, Guid? tenantId, string? actorId, string? reason, CancellationToken cancellationToken = default)
    {
        dbContext.AuditEntries.Add(new AuditEntry
        {
            Id = Guid.NewGuid(),
            EventName = eventName,
            TenantId = tenantId,
            ActorId = actorId,
            Reason = reason,
            OccurredAt = clock.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
