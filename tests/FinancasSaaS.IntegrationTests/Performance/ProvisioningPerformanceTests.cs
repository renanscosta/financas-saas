using System.Diagnostics;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using FinancasSaaS.Platform.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Performance;

public sealed class ProvisioningPerformanceTests
{
    [Fact]
    public async Task ConsultaDeProvisionamentoMantemP95AbaixoDeDoisSegundos()
    {
        var connection = Environment.GetEnvironmentVariable("PLATFORM_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=financas_platform;Username=financas;Password=local-only-change-me";
        var options = new DbContextOptionsBuilder<PlatformDbContext>().UseNpgsql(connection).Options;
        await using var db = new PlatformDbContext(options);
        await db.Database.MigrateAsync();

        var tenantId = Guid.NewGuid();
        var tenantIds = new List<Guid> { tenantId };
        for (var index = 0; index < 250; index++)
        {
            var currentTenantId = index == 0 ? tenantId : Guid.NewGuid();
            var customerId = Guid.NewGuid();
            tenantIds.Add(currentTenantId);
            db.Customers.Add(new CustomerEntity
            {
                Id = customerId,
                Email = $"perf-{customerId:N}@example.com",
                TenantId = currentTenantId,
                Status = Domain.Platform.CustomerStatus.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });
            db.Tenants.Add(new TenantEntity
            {
                Id = currentTenantId,
                CustomerId = customerId,
                DatabaseName = $"perf_{currentTenantId:N}",
                ConnectionSecretReference = "configured",
                Status = Domain.Platform.CustomerStatus.Active,
                ProvisioningStatus = Domain.Platform.ProvisioningStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            });
            db.Provisionings.Add(new ProvisioningEntity
            {
                Id = Guid.NewGuid(),
                TenantId = currentTenantId,
                Status = Domain.Platform.ProvisioningStatus.Pending,
                LastAttemptAt = DateTimeOffset.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var query = db.Provisionings
            .AsNoTracking()
            .Where(item => item.TenantId == tenantId)
            .Select(item => new { item.TenantId, item.Status, item.Attempt, item.LastAttemptAt });
        var sql = query.ToQueryString();
        var timings = new List<double>();
        for (var index = 0; index < 30; index++)
        {
            var timer = Stopwatch.StartNew();
            await query.SingleOrDefaultAsync();
            timer.Stop();
            timings.Add(timer.Elapsed.TotalMilliseconds);
        }

        timings.Sort();
        var p95 = timings[(int)Math.Ceiling(timings.Count * 0.95) - 1];
        Console.WriteLine($"ProvisioningStatus p95={p95:F3}ms; SQL={sql}");
        await using var explainCommand = db.Database.GetDbConnection().CreateCommand();
        explainCommand.CommandText = "EXPLAIN (ANALYZE, BUFFERS) SELECT \"TenantId\", \"Status\", \"Attempt\", \"LastAttemptAt\" FROM \"Provisionings\" WHERE \"TenantId\" = @tenantId";
        explainCommand.Parameters.Add(new NpgsqlParameter("tenantId", tenantId));
        if (explainCommand.Connection!.State != System.Data.ConnectionState.Open)
        {
            await explainCommand.Connection.OpenAsync();
        }

        await using var explainReader = await explainCommand.ExecuteReaderAsync();
        var planLines = new List<string>();
        while (await explainReader.ReadAsync())
        {
            planLines.Add(explainReader.GetString(0));
        }

        Console.WriteLine($"ProvisioningStatus plan={string.Join(" | ", planLines)}");
        var concurrentTimings = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
        {
            await using var concurrentDb = new PlatformDbContext(options);
            var concurrentQuery = concurrentDb.Provisionings
                .AsNoTracking()
                .Where(item => item.TenantId == tenantId)
                .Select(item => item.TenantId);
            var timer = Stopwatch.StartNew();
            await concurrentQuery.SingleOrDefaultAsync();
            timer.Stop();
            return timer.Elapsed.TotalMilliseconds;
        }));
        Console.WriteLine($"ProvisioningStatus concurrentP95={concurrentTimings.OrderBy(item => item).ElementAt(7):F3}ms; tenants={tenantIds.Count}");
        Assert.True(p95 < 2000, $"p95 da consulta excedeu 2s: {p95:F3}ms");
    }
}