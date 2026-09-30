using FinancasSaaS.Application.Platform.Invitations;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Domain.Platform;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Platform.Infrastructure;

public sealed class PlatformInvitationStore(PlatformDbContext dbContext, IAuditWriter auditWriter) : IInvitationStore
{
    public async Task<IssueInvitationResult> CreateAsync(Guid customerId, Guid tenantId, Guid userId, string recipient, string tokenHash, string actorId, DateTimeOffset issuedAt, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var normalizedRecipient = recipient.Trim();
        var recipientPattern = normalizedRecipient
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var eligible = await dbContext.Customers
            .Join(
                dbContext.Tenants,
                customer => customer.TenantId,
                tenant => tenant.Id,
                (customer, tenant) => new { customer, tenant })
            .Join(
                dbContext.Users,
                item => item.tenant.Id,
                user => user.TenantId,
                (item, user) => new { item.customer, item.tenant, user })
            .AnyAsync(item =>
                item.customer.Id == customerId &&
                item.tenant.Id == tenantId &&
                item.user.Id == userId &&
                item.customer.Status == CustomerStatus.Active &&
                item.tenant.CustomerId == item.customer.Id &&
                item.tenant.Status == CustomerStatus.Active &&
                item.tenant.ProvisioningStatus == ProvisioningStatus.Ready &&
                EF.Functions.ILike(item.customer.Email, recipientPattern, "\\") &&
                EF.Functions.ILike(item.user.Email!, recipientPattern, "\\"),
                cancellationToken);
        if (!eligible)
        {
            throw new InvalidOperationException("O cliente não está elegível para receber convite.");
        }

        var active = await dbContext.Invitations
            .Where(item => item.CustomerId == customerId && item.Status == InvitationStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var invitation in active)
        {
            invitation.Status = InvitationStatus.Invalidated;
            invitation.InvalidatedAt = issuedAt;
            invitation.InvalidationReason = "substituted";
            await auditWriter.WriteAsync("invitation.invalidated", tenantId, actorId, "substituted", cancellationToken);
        }

        var entity = new InvitationEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            TenantId = tenantId,
            UserId = userId,
            TokenHash = tokenHash,
            IssuedAt = issuedAt,
            ExpiresAt = expiresAt,
            Status = InvitationStatus.Active
        };
        dbContext.Invitations.Add(entity);
        await auditWriter.WriteAsync("invitation.issued", tenantId, actorId, null, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new IssueInvitationResult(entity.Id, string.Empty, expiresAt);
    }
}
