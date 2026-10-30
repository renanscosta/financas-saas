using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Persistence;

public sealed class PlatformReferentialIntegrityTests
{
    [Fact]
    public async Task TenantSemClienteRelacionadoERejeitado()
    {
        var connection = FinancasSaaS.IntegrationTests.Infrastructure.IntegrationTestConnections.Platform;
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using var db = new PlatformDbContext(options);
        await db.Database.MigrateAsync();

        db.Tenants.Add(new TenantEntity
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            DatabaseName = $"orphan_{Guid.NewGuid():N}",
            ConnectionSecretReference = "configured",
            Status = Domain.Platform.CustomerStatus.Active,
            ProvisioningStatus = Domain.Platform.ProvisioningStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}