using FinancasSaaS.Application.Platform.DeactivateCustomer;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Domain.Platform;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Platform.Infrastructure;

public sealed class PlatformCustomerDeactivationStore(
    PlatformDbContext dbContext,
    IAuditWriter auditWriter) : ICustomerDeactivationStore
{
    public async Task<CustomerDeactivationResult?> DeactivateAsync(
        Guid customerId,
        string actorId,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var customer = await dbContext.Customers
            .SingleOrDefaultAsync(item => item.Id == customerId, cancellationToken);
        if (customer is null || customer.Status == CustomerStatus.Disabled)
        {
            return null;
        }

        var tenant = await dbContext.Tenants
            .SingleOrDefaultAsync(item => item.Id == customer.TenantId, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        customer.Status = CustomerStatus.Disabled;
        customer.DisabledAt = now;
        tenant.Status = CustomerStatus.Disabled;

        var users = await dbContext.Users
            .Where(item => item.TenantId == tenant.Id)
            .ToListAsync(cancellationToken);
        foreach (var user in users)
        {
            user.AccessEnabled = false;
        }

        var invitations = await dbContext.Invitations
            .Where(item => item.CustomerId == customerId && item.Status == InvitationStatus.Active)
            .ToListAsync(cancellationToken);
        foreach (var invitation in invitations)
        {
            invitation.Status = InvitationStatus.Invalidated;
            invitation.InvalidatedAt = now;
            invitation.InvalidationReason = "customer-disabled";
        }

        await auditWriter.WriteAsync("customer.disabled", tenant.Id, actorId, reason, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new CustomerDeactivationResult(customer.Id, tenant.Id);
    }
}