using FinancasSaaS.Finance.Infrastructure.Provisioning;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using FinancasSaaS.Platform.Infrastructure.Tenancy;
using FinancasSaaS.Platform.Infrastructure.Configuration;
using FinancasSaaS.Platform.Infrastructure.Services;
using FinancasSaaS.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class ProvisioningAuditAtomicityTests
{
    [Fact]
    public async Task FalhaNaAuditoriaDeInicioNaoDeixaTenantEmAndamento()
    {
        var connection = Environment.GetEnvironmentVariable("PLATFORM_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=financas_platform;Username=financas;Password=local-only-change-me";
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using var db = new PlatformDbContext(options);
        await db.Database.MigrateAsync();

        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        db.Customers.Add(new CustomerEntity
        {
            Id = customerId,
            Email = $"audit-{customerId:N}@example.com",
            TenantId = tenantId,
            Status = Domain.Platform.CustomerStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            CustomerId = customerId,
            DatabaseName = $"audit_{tenantId:N}",
            ConnectionSecretReference = "configured",
            Status = Domain.Platform.CustomerStatus.Active,
            ProvisioningStatus = Domain.Platform.ProvisioningStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.Provisionings.Add(new ProvisioningEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Status = Domain.Platform.ProvisioningStatus.Pending,
            LastAttemptAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var provisioner = new PostgresTenantProvisioner(
            new PlatformTenantProvisioningCatalog(db, Options.Create(new TenantDatabaseOptions())),
            new PlatformTenantProvisioningStateStore(db, new FailingAuditWriter()),
            new SystemClock());

        await Assert.ThrowsAsync<InvalidOperationException>(() => provisioner.ProvisionAsync(tenantId));

        var tenant = await db.Tenants.SingleAsync(item => item.Id == tenantId);
        Assert.Equal(Domain.Platform.ProvisioningStatus.Failed, tenant.ProvisioningStatus);
        Assert.True(await db.AuditEntries.AnyAsync(item =>
            item.TenantId == tenantId &&
            item.EventName == "tenant.provisioning.failed" &&
            item.ActorId == "system.provisioner"));
    }

    private sealed class FailingAuditWriter : IAuditWriter
    {
        public Task WriteAsync(string eventName, Guid? tenantId, string? actorId, string? reason, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Falha de auditoria simulada.");
    }
}