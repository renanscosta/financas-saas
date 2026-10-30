using FinancasSaaS.Application.Platform.DeactivateCustomer;
using FinancasSaaS.Domain.Platform;
using FinancasSaaS.Platform.Infrastructure;
using FinancasSaaS.Platform.Infrastructure.Configuration;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using FinancasSaaS.Platform.Infrastructure.Tenancy;
using FinancasSaaS.Platform.Infrastructure.Auditing;
using FinancasSaaS.Platform.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class DeactivationIntegrationTests
{
    [Fact]
    public async Task DesativacaoPreservaDadosBloqueiaAcessoEInvalidaConvite()
    {
        var connection = FinancasSaaS.IntegrationTests.Infrastructure.IntegrationTestConnections.Platform;
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using var db = new PlatformDbContext(options);
        await db.Database.MigrateAsync();

        var customerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var email = $"deactivate-{customerId:N}@example.com";
        var userName = $"deactivate-user-{userId:N}";
        var token = $"deactivate-token-{Guid.NewGuid():N}";
        var tokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));

        db.Customers.Add(new CustomerEntity
        {
            Id = customerId,
            Email = email,
            TenantId = tenantId,
            Status = Domain.Platform.CustomerStatus.Active,
            CreatedAt = now
        });
        db.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            CustomerId = customerId,
            DatabaseName = $"deactivate_{tenantId:N}",
            ConnectionSecretReference = "configured",
            Status = Domain.Platform.CustomerStatus.Active,
            ProvisioningStatus = Domain.Platform.ProvisioningStatus.Ready,
            CreatedAt = now
        });
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            TenantId = tenantId,
            AccessEnabled = true
        });
        db.Invitations.Add(new InvitationEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            TenantId = tenantId,
            UserId = userId,
            TokenHash = tokenHash,
            IssuedAt = now,
            ExpiresAt = now.AddHours(24),
            Status = Domain.Platform.InvitationStatus.Active
        });
        await db.SaveChangesAsync();

        var useCase = new DeactivateCustomerUseCase(
            new PlatformCustomerDeactivationStore(db, new PlatformAuditWriter(db, new SystemClock())),
            new SystemClock());
        var result = await useCase.ExecuteAsync(new DeactivateCustomerCommand(customerId, "operator-1", "Solicitação do cliente"));

        var customer = await db.Customers.SingleAsync(item => item.Id == customerId);
        var tenant = await db.Tenants.SingleAsync(item => item.Id == tenantId);
        var user = await db.Users.SingleAsync(item => item.Id == userId);
        var invitation = await db.Invitations.SingleAsync(item => item.TokenHash == tokenHash);
        var resolver = new PlatformTenantConnectionResolver(db, Options.Create(new TenantDatabaseOptions()));

        Assert.NotNull(result);
        Assert.Equal(CustomerStatus.Disabled, customer.Status);
        Assert.Equal(CustomerStatus.Disabled, tenant.Status);
        Assert.True(tenant.ProvisioningStatus == ProvisioningStatus.Ready);
        Assert.False(user.AccessEnabled);
        Assert.Equal(InvitationStatus.Invalidated, invitation.Status);
        Assert.Equal("customer-disabled", invitation.InvalidationReason);
        Assert.True(await db.AuditEntries.AnyAsync(item =>
            item.TenantId == tenantId &&
            item.EventName == "customer.disabled" &&
            item.ActorId == "operator-1"));
        Assert.Null(await resolver.ResolveAsync(userId));
    }
}