using FinancasSaaS.Finance.Infrastructure.Provisioning;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using FinancasSaaS.Platform.Infrastructure.Tenancy;
using FinancasSaaS.Platform.Infrastructure.Configuration;
using FinancasSaaS.Platform.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class ProvisioningFailureTests
{
    [Fact]
    public async Task ConexaoAusenteMarcaTenantComoFalhoEPreservaAcessoBloqueado()
    {
        var connection = FinancasSaaS.IntegrationTests.Infrastructure.IntegrationTestConnections.Platform;
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using var db = new PlatformDbContext(options);
        await db.Database.MigrateAsync();

        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        db.Customers.Add(new CustomerEntity
        {
            Id = customerId,
            Email = $"failure-{customerId:N}@example.com",
            TenantId = tenantId,
            Status = Domain.Platform.CustomerStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });
        db.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            CustomerId = customerId,
            DatabaseName = $"missing_{tenantId:N}",
            ConnectionSecretReference = "missing",
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

        var catalog = new PlatformTenantProvisioningCatalog(
            db,
            Microsoft.Extensions.Options.Options.Create(new TenantDatabaseOptions()));
        var audit = new FinancasSaaS.Platform.Infrastructure.Auditing.PlatformAuditWriter(db, new SystemClock());
        var states = new PlatformTenantProvisioningStateStore(db, audit);
        var provisioner = new PostgresTenantProvisioner(catalog, states, new SystemClock());

        await Assert.ThrowsAsync<InvalidOperationException>(() => provisioner.ProvisionAsync(tenantId));

        var tenant = await db.Tenants.SingleAsync(item => item.Id == tenantId);
        var state = await db.Provisionings.SingleAsync(item => item.TenantId == tenantId);
        Assert.Equal(Domain.Platform.ProvisioningStatus.Failed, tenant.ProvisioningStatus);
        Assert.Equal(Domain.Platform.ProvisioningStatus.Failed, state.Status);
        Assert.False(string.IsNullOrWhiteSpace(state.LastFailure));
    }
}
