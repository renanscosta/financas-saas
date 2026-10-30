using FinancasSaaS.Application.Platform.CompleteRegistration;
using FinancasSaaS.Platform.Infrastructure;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using FinancasSaaS.Platform.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class RegistrationCompletionIntegrationTests
{
    [Fact]
    public async Task ConviteValidoHabilitaUsuarioENaoPodeSerReutilizado()
    {
        var connection = FinancasSaaS.IntegrationTests.Infrastructure.IntegrationTestConnections.Platform;
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using var db = new PlatformDbContext(options);
        await db.Database.MigrateAsync();

        var customerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = $"token-{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;
        db.Customers.Add(new CustomerEntity { Id = customerId, Email = $"invite-{customerId:N}@example.com", TenantId = tenantId, Status = Domain.Platform.CustomerStatus.Active, CreatedAt = now });
        db.Tenants.Add(new TenantEntity { Id = tenantId, CustomerId = customerId, DatabaseName = $"tenant_{tenantId:N}", ConnectionSecretReference = "configured", Status = Domain.Platform.CustomerStatus.Active, ProvisioningStatus = Domain.Platform.ProvisioningStatus.Ready, CreatedAt = now });
        var userName = $"invite-user-{userId:N}";
        db.Users.Add(new ApplicationUser { Id = userId, UserName = userName, NormalizedUserName = userName.ToUpperInvariant(), Email = $"{userName}@example.com", NormalizedEmail = $"{userName}@EXAMPLE.COM", TenantId = tenantId, AccessEnabled = false });
        db.Invitations.Add(new InvitationEntity { Id = Guid.NewGuid(), CustomerId = customerId, TenantId = tenantId, UserId = userId, TokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))), IssuedAt = now, ExpiresAt = now.AddHours(24), Status = Domain.Platform.InvitationStatus.Active });
        await db.SaveChangesAsync();

        var store = new PlatformRegistrationCompletionStore(db, new PasswordHasher<ApplicationUser>(), new FinancasSaaS.Platform.Infrastructure.Auditing.PlatformAuditWriter(db, new SystemClock()));
        var useCase = new CompleteRegistrationUseCase(store, new Sha256TokenHasher(), new SystemClock());
        var result = await useCase.ExecuteAsync(new CompleteRegistrationCommand(token, "StrongPassword123!"));
        var reused = await useCase.ExecuteAsync(new CompleteRegistrationCommand(token, "StrongPassword123!"));

        Assert.NotNull(result);
        Assert.Null(reused);
        Assert.True((await db.Users.SingleAsync(item => item.Id == userId)).AccessEnabled);
        Assert.True(await db.AuditEntries.AnyAsync(item => item.TenantId == tenantId && item.EventName == "invitation.used" && item.ActorId == userId.ToString()));
    }

    [Fact]
    public async Task ConviteNaoHabilitaAcessoComTenantPendente()
    {
        var connection = FinancasSaaS.IntegrationTests.Infrastructure.IntegrationTestConnections.Platform;
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using var db = new PlatformDbContext(options);
        await db.Database.MigrateAsync();
        var customerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = $"pending-{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;
        db.Customers.Add(new CustomerEntity { Id = customerId, Email = $"pending-{customerId:N}@example.com", TenantId = tenantId, Status = Domain.Platform.CustomerStatus.Active, CreatedAt = now });
        db.Tenants.Add(new TenantEntity { Id = tenantId, CustomerId = customerId, DatabaseName = $"pending_{tenantId:N}", ConnectionSecretReference = "configured", Status = Domain.Platform.CustomerStatus.Active, ProvisioningStatus = Domain.Platform.ProvisioningStatus.Pending, CreatedAt = now });
        var userName = $"pending-user-{userId:N}";
        db.Users.Add(new ApplicationUser { Id = userId, UserName = userName, NormalizedUserName = userName.ToUpperInvariant(), TenantId = tenantId, AccessEnabled = false });
        db.Invitations.Add(new InvitationEntity { Id = Guid.NewGuid(), CustomerId = customerId, TenantId = tenantId, UserId = userId, TokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))), IssuedAt = now, ExpiresAt = now.AddHours(24), Status = Domain.Platform.InvitationStatus.Active });
        await db.SaveChangesAsync();

        var useCase = new CompleteRegistrationUseCase(new PlatformRegistrationCompletionStore(db, new PasswordHasher<ApplicationUser>(), new FinancasSaaS.Platform.Infrastructure.Auditing.PlatformAuditWriter(db, new SystemClock())), new Sha256TokenHasher(), new SystemClock());
        var result = await useCase.ExecuteAsync(new CompleteRegistrationCommand(token, "StrongPassword123!"));

        Assert.Null(result);
        Assert.False((await db.Users.SingleAsync(item => item.Id == userId)).AccessEnabled);
    }
}
