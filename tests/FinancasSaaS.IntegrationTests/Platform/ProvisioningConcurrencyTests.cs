using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Finance.Infrastructure.Provisioning;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using FinancasSaaS.Platform.Infrastructure.Tenancy;
using FinancasSaaS.Platform.Infrastructure.Configuration;
using FinancasSaaS.Platform.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class ProvisioningConcurrencyTests
{
    [Fact]
    public async Task DuasRetentativasDoMesmoTenantPermitemSomenteUmaExecucao()
    {
        var connection = Environment.GetEnvironmentVariable("PLATFORM_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=financas_platform;Username=financas;Password=local-only-change-me";
        var tenantId = Guid.NewGuid();
        var setupOptions = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using (var setup = new PlatformDbContext(setupOptions))
        {
            await setup.Database.MigrateAsync();
            var customerId = Guid.NewGuid();
            setup.Customers.Add(new CustomerEntity
            {
                Id = customerId,
                Email = $"concurrent-{customerId:N}@example.com",
                TenantId = tenantId,
                Status = Domain.Platform.CustomerStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });
            setup.Tenants.Add(new TenantEntity
            {
                Id = tenantId,
                CustomerId = customerId,
                DatabaseName = $"concurrent_{tenantId:N}",
                ConnectionSecretReference = "configured",
                Status = Domain.Platform.CustomerStatus.Active,
                ProvisioningStatus = Domain.Platform.ProvisioningStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            });
            setup.Provisionings.Add(new ProvisioningEntity
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Status = Domain.Platform.ProvisioningStatus.Pending,
                LastAttemptAt = DateTimeOffset.UtcNow
            });
            await setup.SaveChangesAsync();
        }

        await using var first = new PlatformDbContext(new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options);
        await using var second = new PlatformDbContext(new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options);
        var firstStore = new PlatformTenantProvisioningStateStore(first, new NoOpAuditWriter());
        var secondStore = new PlatformTenantProvisioningStateStore(second, new NoOpAuditWriter());

        var results = await Task.WhenAll(
            firstStore.BeginAsync(tenantId, DateTimeOffset.UtcNow),
            secondStore.BeginAsync(tenantId, DateTimeOffset.UtcNow));

        Assert.Single(results, result => result);
    }

    private sealed class NoOpAuditWriter : IAuditWriter
    {
        public Task WriteAsync(string eventName, Guid? tenantId, string? actorId, string? reason, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}