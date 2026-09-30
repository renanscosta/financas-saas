using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Finance.Infrastructure.Provisioning;
using FinancasSaaS.Platform.Infrastructure.Configuration;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using FinancasSaaS.Platform.Infrastructure.Services;
using FinancasSaaS.Platform.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class ProvisioningIntegrationTests
{
    [Fact]
    public async Task ProvisionamentoCriaCategoriasEMarcaTenantPronto()
    {
        var platformConnection = "Host=localhost;Port=5432;Database=financas_platform;Username=financas;Password=local-only-change-me";
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var databaseName = $"financas_tenant_{tenantId:N}";
        var tenantConnection = $"Host=localhost;Port=5432;Database={databaseName};Username=financas;Password=local-only-change-me";
        var platformOptions = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(platformConnection).Options;

        await using var platformDb = new PlatformDbContext(platformOptions);
        await platformDb.Database.MigrateAsync();
        platformDb.Customers.Add(new CustomerEntity
        {
            Id = customerId,
            Email = $"provision-{customerId:N}@example.com",
            TenantId = tenantId,
            Status = Domain.Platform.CustomerStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });
        platformDb.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            CustomerId = customerId,
            DatabaseName = databaseName,
            ConnectionSecretReference = databaseName,
            Status = Domain.Platform.CustomerStatus.Active,
            ProvisioningStatus = Domain.Platform.ProvisioningStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        });
        platformDb.Provisionings.Add(new ProvisioningEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Status = Domain.Platform.ProvisioningStatus.Pending,
            LastAttemptAt = DateTimeOffset.UtcNow
        });
        await platformDb.SaveChangesAsync();

        var options = Options.Create(new TenantDatabaseOptions
        {
            Connections = new Dictionary<string, string> { [databaseName] = tenantConnection }
        });
        var catalog = new PlatformTenantProvisioningCatalog(platformDb, options);
        var audit = new FinancasSaaS.Platform.Infrastructure.Auditing.PlatformAuditWriter(platformDb, new SystemClock());
        var stateStore = new PlatformTenantProvisioningStateStore(platformDb, audit);
        var provisioner = new PostgresTenantProvisioner(catalog, stateStore, new SystemClock());

        await provisioner.ProvisionAsync(tenantId);
        await provisioner.ProvisionAsync(tenantId);

        var tenant = await platformDb.Tenants.SingleAsync(item => item.Id == tenantId);
        Assert.Equal(Domain.Platform.ProvisioningStatus.Ready, tenant.ProvisioningStatus);

        var tenantOptions = new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(tenantConnection).Options;
        await using var tenantDb = new TenantDbContext(tenantOptions);
        Assert.Equal(3, await tenantDb.Categories.CountAsync());
    }
}
