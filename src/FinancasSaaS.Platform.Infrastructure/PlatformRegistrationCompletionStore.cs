using FinancasSaaS.Application.Platform.CompleteRegistration;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Domain.Platform;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Platform.Infrastructure;

public sealed class PlatformRegistrationCompletionStore(
    PlatformDbContext dbContext,
    IPasswordHasher<ApplicationUser> passwordHasher,
    IAuditWriter auditWriter) : IRegistrationCompletionStore
{
    public async Task<CompleteRegistrationResult?> CompleteAsync(string tokenHash, string password, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var invitation = await dbContext.Invitations.SingleOrDefaultAsync(item => item.TokenHash == tokenHash, cancellationToken);
        if (invitation is null || invitation.Status != InvitationStatus.Active || invitation.ExpiresAt <= now)
        {
            return null;
        }

        var customer = await dbContext.Customers.SingleOrDefaultAsync(item => item.Id == invitation.CustomerId, cancellationToken);
        var tenant = await dbContext.Tenants.SingleOrDefaultAsync(item => item.Id == invitation.TenantId, cancellationToken);
        var user = await dbContext.Users.SingleOrDefaultAsync(item => item.Id == invitation.UserId, cancellationToken);
        if (customer is null || tenant is null || user is null || customer.Status != CustomerStatus.Active || tenant.Status != CustomerStatus.Active || tenant.ProvisioningStatus != ProvisioningStatus.Ready)
        {
            return null;
        }

        user.PasswordHash = passwordHasher.HashPassword(user, password);
        user.EmailConfirmed = true;
        user.AccessEnabled = true;
        invitation.Status = InvitationStatus.Used;
        invitation.UsedAt = now;
        await auditWriter.WriteAsync("invitation.used", tenant.Id, user.Id.ToString(), null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CompleteRegistrationResult(user.Id, tenant.Id);
    }
}