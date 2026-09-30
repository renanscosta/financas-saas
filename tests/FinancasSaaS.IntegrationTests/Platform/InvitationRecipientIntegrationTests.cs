using FinancasSaaS.Application.Platform.Invitations;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Domain.Platform;
using FinancasSaaS.Platform.Infrastructure;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class InvitationRecipientIntegrationTests
{
    [Fact]
    public async Task DestinatarioDivergenteNaoRecebeConviteNemInvalidaOAnterior()
    {
        var connection = Environment.GetEnvironmentVariable("PLATFORM_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=financas_platform;Username=financas;Password=local-only-change-me";
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using var db = new PlatformDbContext(options);
        await db.Database.MigrateAsync();

        var customerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var email = $"recipient-{customerId:N}@example.com";
        var userName = $"recipient-user-{userId:N}";
        var oldTokenHash = $"old-{Guid.NewGuid():N}";
        db.Customers.Add(new CustomerEntity { Id = customerId, Email = email, TenantId = tenantId, Status = CustomerStatus.Active, CreatedAt = now });
        db.Tenants.Add(new TenantEntity { Id = tenantId, CustomerId = customerId, DatabaseName = $"recipient_{tenantId:N}", ConnectionSecretReference = "configured", Status = CustomerStatus.Active, ProvisioningStatus = ProvisioningStatus.Ready, CreatedAt = now });
        db.Users.Add(new ApplicationUser { Id = userId, UserName = userName, NormalizedUserName = userName.ToUpperInvariant(), Email = email, NormalizedEmail = email.ToUpperInvariant(), TenantId = tenantId, AccessEnabled = false });
        db.Invitations.Add(new InvitationEntity { Id = Guid.NewGuid(), CustomerId = customerId, TenantId = tenantId, UserId = userId, TokenHash = oldTokenHash, IssuedAt = now, ExpiresAt = now.AddHours(24), Status = InvitationStatus.Active });
        await db.SaveChangesAsync();

        var useCase = new SendInvitationUseCase(
            new IssueInvitationUseCase(new PlatformInvitationStore(db, new FinancasSaaS.Platform.Infrastructure.Auditing.PlatformAuditWriter(db, new FinancasSaaS.Platform.Infrastructure.Services.SystemClock())), new FixedTokenGenerator(), new FixedHasher(), new FixedClock()),
            new NoOpEmailSender());

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(new SendInvitationCommand(customerId, tenantId, userId, "other@example.com", "https://app.local")));

        var invitation = await db.Invitations.SingleAsync(item => item.TokenHash == oldTokenHash);
        Assert.Equal(InvitationStatus.Active, invitation.Status);

        await useCase.ExecuteAsync(new SendInvitationCommand(customerId, tenantId, userId, email.ToUpperInvariant(), "https://app.local", "operator-1"));

        invitation = await db.Invitations.SingleAsync(item => item.TokenHash == oldTokenHash);
        Assert.Equal(InvitationStatus.Invalidated, invitation.Status);
        Assert.True(await db.AuditEntries.AnyAsync(item => item.TenantId == tenantId && item.EventName == "invitation.invalidated" && item.ActorId == "operator-1"));
        Assert.True(await db.AuditEntries.AnyAsync(item => item.TenantId == tenantId && item.EventName == "invitation.issued" && item.ActorId == "operator-1"));
    }

    private sealed class FixedTokenGenerator : ITokenGenerator
    {
        public string Generate() => $"new-token-{Guid.NewGuid():N}";
    }

    private sealed class FixedHasher : ITokenHasher
    {
        public string Hash(string token) => $"hash:{token}";
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class NoOpEmailSender : FinancasSaaS.Application.Abstractions.IEmailSender
    {
        public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}