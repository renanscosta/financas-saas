using FinancasSaaS.Domain.Platform;
using FinancasSaaS.Platform.Infrastructure;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class InvitationEligibilityIntegrationTests
{
    [Fact]
    public async Task ConviteNaoPodeSerCriadoParaTenantNaoProntoOuVinculoInconsistente()
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
        db.Customers.Add(new CustomerEntity
        {
            Id = customerId,
            Email = $"eligibility-{customerId:N}@example.com",
            TenantId = tenantId,
            Status = CustomerStatus.Active,
            CreatedAt = now
        });
        db.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            CustomerId = customerId,
            DatabaseName = $"eligibility_{tenantId:N}",
            ConnectionSecretReference = "configured",
            Status = CustomerStatus.Active,
            ProvisioningStatus = ProvisioningStatus.Pending,
            CreatedAt = now
        });
        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = $"eligibility-user-{userId:N}",
            NormalizedUserName = $"ELIGIBILITY-USER-{userId:N}",
            TenantId = tenantId,
            AccessEnabled = false
        });
        await db.SaveChangesAsync();

        var store = new PlatformInvitationStore(db, new FinancasSaaS.Platform.Infrastructure.Auditing.PlatformAuditWriter(db, new FinancasSaaS.Platform.Infrastructure.Services.SystemClock()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.CreateAsync(
            customerId,
            tenantId,
            userId,
                "other@example.com",
            "hash",
                "operator",
            now,
            now.AddHours(24)));
    }
}