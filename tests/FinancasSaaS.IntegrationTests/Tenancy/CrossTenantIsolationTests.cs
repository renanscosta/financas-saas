using FinancasSaaS.Domain.Platform;
using FinancasSaaS.Platform.Infrastructure.Configuration;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using FinancasSaaS.Platform.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Tenancy;

public sealed class CrossTenantIsolationTests
{
    [Fact]
    public async Task ResolucaoServerSideIgnoraTenantManipuladoEEntregaSomenteConexaoDoUsuario()
    {
        var connection = FinancasSaaS.IntegrationTests.Infrastructure.IntegrationTestConnections.Platform;
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using var db = new PlatformDbContext(options);
        await db.Database.MigrateAsync();

        var first = CreateTenantFixture("first");
        var second = CreateTenantFixture("second");
        db.Customers.AddRange(first.Customer, second.Customer);
        db.Tenants.AddRange(first.Tenant, second.Tenant);
        db.Users.AddRange(first.User, second.User);
        await db.SaveChangesAsync();

        var resolver = new PlatformTenantConnectionResolver(
            db,
            Options.Create(new TenantDatabaseOptions
            {
                Connections = new Dictionary<string, string>
                {
                    [first.Tenant.DatabaseName] = "Host=first-tenant",
                    [second.Tenant.DatabaseName] = "Host=second-tenant"
                }
            }));

        var firstConnection = await resolver.ResolveAsync(first.User.Id);
        var secondConnection = await resolver.ResolveAsync(second.User.Id);

        Assert.Equal("Host=first-tenant", firstConnection);
        Assert.Equal("Host=second-tenant", secondConnection);
        Assert.NotEqual(firstConnection, secondConnection);
    }

    [Fact]
    public async Task ClienteDesativadoNaoRecebeConexaoMesmoComTenantAtivo()
    {
        var connection = FinancasSaaS.IntegrationTests.Infrastructure.IntegrationTestConnections.Platform;
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using var db = new PlatformDbContext(options);
        await db.Database.MigrateAsync();

        var fixture = CreateTenantFixture("disabled-customer");
        fixture.Customer.Status = CustomerStatus.Disabled;
        db.Customers.Add(fixture.Customer);
        db.Tenants.Add(fixture.Tenant);
        db.Users.Add(fixture.User);
        await db.SaveChangesAsync();

        var resolver = new PlatformTenantConnectionResolver(
            db,
            Options.Create(new TenantDatabaseOptions
            {
                Connections = new Dictionary<string, string>
                {
                    [fixture.Tenant.DatabaseName] = "Host=disabled-customer"
                }
            }));

        Assert.Null(await resolver.ResolveAsync(fixture.User.Id));
    }

    private static (CustomerEntity Customer, TenantEntity Tenant, ApplicationUser User) CreateTenantFixture(string prefix)
    {
        var customerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var email = $"{prefix}-{customerId:N}@example.com";
        var userName = $"{prefix}-user-{userId:N}";
        var databaseName = $"{prefix}_{tenantId:N}";

        return (
            new CustomerEntity
            {
                Id = customerId,
                Email = email,
                TenantId = tenantId,
                Status = CustomerStatus.Active,
                CreatedAt = now
            },
            new TenantEntity
            {
                Id = tenantId,
                CustomerId = customerId,
                DatabaseName = databaseName,
                ConnectionSecretReference = "configured",
                Status = CustomerStatus.Active,
                ProvisioningStatus = ProvisioningStatus.Ready,
                CreatedAt = now
            },
            new ApplicationUser
            {
                Id = userId,
                UserName = userName,
                NormalizedUserName = userName.ToUpperInvariant(),
                Email = email,
                NormalizedEmail = email.ToUpperInvariant(),
                TenantId = tenantId,
                AccessEnabled = true
            });
    }
}