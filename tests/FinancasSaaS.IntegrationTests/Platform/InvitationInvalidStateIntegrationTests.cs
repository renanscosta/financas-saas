using FinancasSaaS.Application.Platform.CompleteRegistration;
using FinancasSaaS.Domain.Platform;
using FinancasSaaS.Platform.Infrastructure;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using FinancasSaaS.Platform.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class InvitationInvalidStateIntegrationTests
{
    [Theory]
    [InlineData(InvitationStatus.Expired)]
    [InlineData(InvitationStatus.Invalidated)]
    [InlineData(InvitationStatus.Used)]
    public async Task ConviteInvalidoNaoHabilitaUsuario(InvitationStatus status)
    {
        var connection = "Host=localhost;Port=5432;Database=financas_platform;Username=financas;Password=local-only-change-me";
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using var db = new PlatformDbContext(options);
        await db.Database.MigrateAsync();

        var customerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var token = $"invalid-{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;
        db.Customers.Add(new CustomerEntity { Id = customerId, Email = $"invalid-{customerId:N}@example.com", TenantId = tenantId, Status = CustomerStatus.Active, CreatedAt = now });
        db.Tenants.Add(new TenantEntity { Id = tenantId, CustomerId = customerId, DatabaseName = $"invalid_{tenantId:N}", ConnectionSecretReference = "configured", Status = CustomerStatus.Active, ProvisioningStatus = ProvisioningStatus.Ready, CreatedAt = now });
        var userName = $"invalid-user-{userId:N}";
        db.Users.Add(new ApplicationUser { Id = userId, UserName = userName, NormalizedUserName = userName.ToUpperInvariant(), TenantId = tenantId, AccessEnabled = false });
        db.Invitations.Add(new InvitationEntity { Id = Guid.NewGuid(), CustomerId = customerId, TenantId = tenantId, UserId = userId, TokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))), IssuedAt = now.AddHours(-25), ExpiresAt = now.AddHours(-1), Status = status });
        await db.SaveChangesAsync();

        var useCase = new CompleteRegistrationUseCase(new PlatformRegistrationCompletionStore(db, new PasswordHasher<ApplicationUser>(), new FinancasSaaS.Platform.Infrastructure.Auditing.PlatformAuditWriter(db, new SystemClock())), new Sha256TokenHasher(), new SystemClock());
        var result = await useCase.ExecuteAsync(new CompleteRegistrationCommand(token, "StrongPassword123!"));

        Assert.Null(result);
        Assert.False((await db.Users.SingleAsync(item => item.Id == userId)).AccessEnabled);
    }
}
