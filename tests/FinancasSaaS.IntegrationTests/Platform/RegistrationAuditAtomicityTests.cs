using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Platform.Infrastructure;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class RegistrationAuditAtomicityTests
{
    [Fact]
    public async Task FalhaAoAuditarNaoConfirmaCadastroParcial()
    {
        var connection = Environment.GetEnvironmentVariable("PLATFORM_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=financas_platform;Username=financas;Password=local-only-change-me";
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using var db = new PlatformDbContext(options);
        await db.Database.MigrateAsync();

        var email = $"audit-atomic-{Guid.NewGuid():N}@example.com".ToUpperInvariant();
        var store = new PlatformCustomerRegistrationStore(
            db,
            new FinancasSaaS.Platform.Infrastructure.Services.SystemClock(),
            new FailingAuditWriter());

        await Assert.ThrowsAsync<InvalidOperationException>(() => store.CreateAsync(email, "operator-1"));

        Assert.False(await db.Customers.AnyAsync(item => item.Email == email));
        Assert.False(await db.Users.AnyAsync(item => item.Email == email));
    }

    private sealed class FailingAuditWriter : IAuditWriter
    {
        public Task WriteAsync(string eventName, Guid? tenantId, string? actorId, string? reason, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Falha de auditoria simulada.");
    }
}