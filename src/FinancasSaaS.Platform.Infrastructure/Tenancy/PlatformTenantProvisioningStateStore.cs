using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Domain.Platform;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Platform.Infrastructure.Tenancy;

public sealed class PlatformTenantProvisioningStateStore(
    PlatformDbContext dbContext,
    IAuditWriter auditWriter) : ITenantProvisioningStateStore
{
    private const string SystemActor = "system.provisioner";

    public async Task<bool> BeginAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var updated = await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "Tenants"
            SET "ProvisioningStatus" = 'InProgress'
            WHERE "Id" = {tenantId}
              AND "ProvisioningStatus" NOT IN ('Ready', 'InProgress')
            """, cancellationToken);
        if (updated == 0)
        {
            if (!await dbContext.Tenants.AnyAsync(item => item.Id == tenantId, cancellationToken))
            {
                throw new InvalidOperationException("Tenant não encontrado.");
            }

            return false;
        }

        var state = await dbContext.Provisionings.SingleAsync(item => item.TenantId == tenantId, cancellationToken);
        state.Status = ProvisioningStatus.InProgress;
        state.Attempt++;
        state.StartedAt = now;
        state.LastAttemptAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
        try
        {
            await auditWriter.WriteAsync("tenant.provisioning.started", tenantId, SystemActor, null, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await RecoverFailureAsync(tenantId, now, "Falha ao registrar o início do provisionamento.", cancellationToken);
            throw;
        }
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task MarkReadyAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tenant = await dbContext.Tenants.SingleAsync(item => item.Id == tenantId, cancellationToken);
        var state = await dbContext.Provisionings.SingleAsync(item => item.TenantId == tenantId, cancellationToken);
        tenant.ProvisioningStatus = ProvisioningStatus.Ready;
        state.Status = ProvisioningStatus.Ready;
        state.CompletedAt = now;
        state.LastFailure = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        try
        {
            await auditWriter.WriteAsync("tenant.provisioning.completed", tenantId, SystemActor, null, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await RecoverFailureAsync(tenantId, now, "Falha ao registrar a conclusão do provisionamento.", cancellationToken);
            throw;
        }
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid tenantId, DateTimeOffset now, string failure, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tenant = await dbContext.Tenants.SingleAsync(item => item.Id == tenantId, cancellationToken);
        var state = await dbContext.Provisionings.SingleAsync(item => item.TenantId == tenantId, cancellationToken);
        tenant.ProvisioningStatus = ProvisioningStatus.Failed;
        state.Status = ProvisioningStatus.Failed;
        state.LastAttemptAt = now;
        state.LastFailure = failure[..Math.Min(failure.Length, 2000)];
        await dbContext.SaveChangesAsync(cancellationToken);
        try
        {
            await auditWriter.WriteAsync("tenant.provisioning.failed", tenantId, SystemActor, "Falha registrada no provisionamento.", cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await RecoverFailureAsync(tenantId, now, "Falha ao registrar a falha do provisionamento.", cancellationToken);
            throw;
        }
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task RecoverFailureAsync(Guid tenantId, DateTimeOffset now, string reason, CancellationToken cancellationToken)
    {
        dbContext.ChangeTracker.Clear();
        await using var recoveryTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var tenant = await dbContext.Tenants.SingleAsync(item => item.Id == tenantId, cancellationToken);
        var state = await dbContext.Provisionings.SingleAsync(item => item.TenantId == tenantId, cancellationToken);
        tenant.ProvisioningStatus = ProvisioningStatus.Failed;
        state.Status = ProvisioningStatus.Failed;
        state.LastAttemptAt = now;
        state.LastFailure = reason;
        dbContext.AuditEntries.Add(new Persistence.Entities.AuditEntry
        {
            Id = Guid.NewGuid(),
            EventName = "tenant.provisioning.failed",
            TenantId = tenantId,
            ActorId = SystemActor,
            Reason = reason,
            OccurredAt = now
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        await recoveryTransaction.CommitAsync(cancellationToken);
    }
}