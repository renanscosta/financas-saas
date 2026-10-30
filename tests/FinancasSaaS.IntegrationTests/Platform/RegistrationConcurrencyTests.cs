using FinancasSaaS.Application.Platform.RegisterCustomer;
using FinancasSaaS.Platform.Infrastructure;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class RegistrationConcurrencyTests
{
    [Fact]
    public async Task CadastrosConcorrentesPreservamUmUnicoClienteETenant()
    {
        var connection = FinancasSaaS.IntegrationTests.Infrastructure.IntegrationTestConnections.Platform;
        var setupOptions = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using (var setup = new PlatformDbContext(setupOptions))
        {
            await setup.Database.MigrateAsync();
        }

        await using var firstDb = new PlatformDbContext(new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options);
        await using var secondDb = new PlatformDbContext(new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options);
        var first = new RegisterCustomerUseCase(
            new PlatformCustomerRegistrationStore(firstDb, new SystemClock(), new FinancasSaaS.Platform.Infrastructure.Auditing.PlatformAuditWriter(firstDb, new SystemClock())));
        var second = new RegisterCustomerUseCase(
            new PlatformCustomerRegistrationStore(secondDb, new SystemClock(), new FinancasSaaS.Platform.Infrastructure.Auditing.PlatformAuditWriter(secondDb, new SystemClock())));
        var email = $"race-{Guid.NewGuid():N}@example.com";

        var results = await Task.WhenAll(
            first.ExecuteAsync(new RegisterCustomerCommand(email, "admin-a")),
            second.ExecuteAsync(new RegisterCustomerCommand(email, "admin-b")));

        Assert.Single(results.Select(item => item.CustomerId).Distinct());
        Assert.Single(results.Select(item => item.TenantId).Distinct());
        Assert.Contains(results, item => !item.AlreadyExists);
        Assert.Contains(results, item => item.AlreadyExists);
    }
}
