using System.Text.Json;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Finance.Infrastructure.Provisioning;
using FinancasSaaS.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Persistence;

public sealed class TenantFinanceMigrationRunnerTests(TenantDatabaseFixture fixture)
    : IClassFixture<TenantDatabaseFixture>
{
    [Fact]
    public async Task MigrationInicialSemeiaUmaVezEExecutarNovamenteRespeitaEdicoesDoCliente()
    {
        var (tenantId, connectionString) = await fixture.CreateTemporaryTenantDatabaseAsync();
        try
        {
            var catalog = new MutableCatalog(
            [
                new TenantFinanceMigrationTarget(tenantId, "temporary", connectionString)
            ]);
            var runner = new TenantFinanceMigrationRunner(catalog, new FixedClock());

            var inventory = await runner.CreateInventoryAsync();
            var json = JsonSerializer.Serialize(inventory);

            Assert.Equal(DateTimeOffset.UnixEpoch, inventory.CapturedAtUtc);
            Assert.Equal([tenantId], inventory.TenantIds);
            Assert.DoesNotContain(connectionString, json, StringComparison.Ordinal);
            Assert.DoesNotContain("Password", json, StringComparison.OrdinalIgnoreCase);

            var progress = new List<string>();
            await runner.MigrateAsync(inventory, (_, status) => progress.Add(status));

            await using (var db = CreateTenantDb(connectionString))
            {
                Assert.Equal(3, await db.Categories.CountAsync());
                var renamed = await db.Categories.SingleAsync(item => item.Name == "Lanches");
                renamed.Name = "Minha alimentação";
                db.Categories.Remove(await db.Categories.SingleAsync(item => item.Name == "Mercado"));
                await db.SaveChangesAsync();
            }

            var result = await runner.MigrateAsync(inventory);

            await using var verify = CreateTenantDb(connectionString);
            var categoryNames = await verify.Categories.Select(item => item.Name).ToArrayAsync();
            Assert.Equal(["Minha alimentação", "Pessoal"], categoryNames.Order().ToArray());
            Assert.Equal(["iniciado", "concluído"], progress);
            Assert.Equal([tenantId], result);
            Assert.DoesNotContain(connectionString, JsonSerializer.Serialize(result), StringComparison.Ordinal);
        }
        finally
        {
            await fixture.DeleteTemporaryTenantDatabaseAsync(tenantId);
        }
    }

    [Fact]
    public async Task MigrationInicialReativaCategoriaPadraoPreexistentePreservandoIdENome()
    {
        var (tenantId, connectionString) = await fixture.CreateTemporaryTenantDatabaseAsync();
        try
        {
            var categoryId = Guid.NewGuid();
            await using (var db = CreateTenantDb(connectionString))
            {
                await db.Database.MigrateAsync("20260929202021_TenantInitial");
                await db.Database.ExecuteSqlRawAsync(
                    """INSERT INTO "Categories" ("Id", "Name", "IsActive") VALUES ({0}, {1}, FALSE)""",
                    categoryId,
                    "lAnChEs");
                await db.Database.MigrateAsync();
            }

            await using var verify = CreateTenantDb(connectionString);
            var category = await verify.Categories.SingleAsync(item => item.Id == categoryId);
            Assert.Equal("lAnChEs", category.Name);
            Assert.True(category.IsActive);
            Assert.Equal(3, await verify.Categories.CountAsync());
        }
        finally
        {
            await fixture.DeleteTemporaryTenantDatabaseAsync(tenantId);
        }
    }

    [Fact]
    public async Task MigrationForwardReativaCategoriaExistenteSemRecriarCategoriasExcluidasOuRenomeadas()
    {
        var (tenantId, connectionString) = await fixture.CreateTemporaryTenantDatabaseAsync();
        try
        {
            await using (var db = CreateTenantDb(connectionString))
            {
                await db.Database.MigrateAsync("20261028032258_ManualTransactionsAndNormalizedCategories");
                var mercado = await db.Categories.SingleAsync(item => item.Name == "Mercado");
                mercado.IsActive = false;
                db.Categories.Remove(await db.Categories.SingleAsync(item => item.Name == "Lanches"));
                var pessoal = await db.Categories.SingleAsync(item => item.Name == "Pessoal");
                pessoal.Name = "Minha organização";
                await db.SaveChangesAsync();

                await db.Database.MigrateAsync();
            }

            await using var verify = CreateTenantDb(connectionString);
            Assert.True((await verify.Categories.SingleAsync(item => item.Name == "Mercado")).IsActive);
            Assert.DoesNotContain(await verify.Categories.Select(item => item.Name).ToArrayAsync(), name => name == "Lanches");
            Assert.Contains(await verify.Categories.Select(item => item.Name).ToArrayAsync(), name => name == "Minha organização");
        }
        finally
        {
            await fixture.DeleteTemporaryTenantDatabaseAsync(tenantId);
        }
    }

    [Fact]
    public async Task AuditoriaListaEstadoDaMigrationEDasCategoriasSemConnectionString()
    {
        var (tenantId, connectionString) = await fixture.CreateTemporaryTenantDatabaseAsync();
        try
        {
            await using (var db = CreateTenantDb(connectionString))
            {
                await db.Database.MigrateAsync("20260929202021_TenantInitial");
                await db.Database.ExecuteSqlRawAsync(
                    """INSERT INTO "Categories" ("Id", "Name", "IsActive") VALUES ({0}, {1}, FALSE)""",
                    Guid.NewGuid(),
                    "Lanches");
            }

            var catalog = new MutableCatalog(
            [
                new TenantFinanceMigrationTarget(tenantId, "temporary", connectionString)
            ]);
            var runner = new TenantFinanceMigrationRunner(catalog, new FixedClock());
            var audit = await runner.AuditAsync(new TenantFinanceInventory(DateTimeOffset.UnixEpoch, [tenantId]));
            var tenant = Assert.Single(audit.Tenants);

            Assert.False(Assert.Single(tenant.FinanceMigrations, item =>
                item.MigrationId == "20261028032258_ManualTransactionsAndNormalizedCategories").Applied);
            Assert.False(Assert.Single(tenant.FinanceMigrations, item =>
                item.MigrationId == "20261029181614_ReactivateExistingDefaultCategories").Applied);
            Assert.Equal("inactive", Assert.Single(tenant.DefaultCategories, item => item.Name == "Lanches").State);
            Assert.Equal("missing", Assert.Single(tenant.DefaultCategories, item => item.Name == "Mercado").State);
            Assert.DoesNotContain(connectionString, JsonSerializer.Serialize(audit), StringComparison.Ordinal);
        }
        finally
        {
            await fixture.DeleteTemporaryTenantDatabaseAsync(tenantId);
        }
    }

    [Fact]
    public async Task InventarioDivergenteDepoisDaMigrationFalhaMesmoComMigraçãoAplicada()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        var expectedTarget = new TenantFinanceMigrationTarget(
            fixture.FirstTenantId,
            "first",
            fixture.FirstConnectionString);
        var extraTarget = new TenantFinanceMigrationTarget(
            fixture.SecondTenantId,
            "second",
            fixture.SecondConnectionString);
        var catalog = new ChangingCatalog([expectedTarget], [expectedTarget, extraTarget]);
        var runner = new TenantFinanceMigrationRunner(catalog, new FixedClock());
        var inventory = new TenantFinanceInventory(DateTimeOffset.UtcNow, [fixture.FirstTenantId]);

        await Assert.ThrowsAsync<TenantFinanceMigrationException>(() => runner.MigrateAsync(inventory));

        await using var db = CreateTenantDb(fixture.FirstConnectionString);
        Assert.Empty(await db.Categories.ToListAsync());
    }

    [Fact]
    public async Task FalhaParcialParaEPermiteRetentativaSegura()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await TenantDatabaseFixture.ResetTenantAsync(fixture.SecondConnectionString);
        var firstTarget = new TenantFinanceMigrationTarget(
            fixture.FirstTenantId,
            "first",
            fixture.FirstConnectionString);
        var secondTarget = new TenantFinanceMigrationTarget(
            Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            "unconfigured",
            null);
        var catalog = new MutableCatalog([firstTarget, secondTarget]);
        var runner = new TenantFinanceMigrationRunner(catalog, new FixedClock());
        var inventory = new TenantFinanceInventory(
            DateTimeOffset.UtcNow,
            [fixture.FirstTenantId, secondTarget.TenantId]);

        await Assert.ThrowsAsync<TenantFinanceMigrationException>(() => runner.MigrateAsync(inventory));

        catalog.Targets =
        [
            firstTarget,
            secondTarget with { ConnectionString = fixture.SecondConnectionString }
        ];
        var migrated = await runner.MigrateAsync(inventory);

        Assert.Equal(2, migrated.Count);
        await using var firstDb = CreateTenantDb(fixture.FirstConnectionString);
        await using var secondDb = CreateTenantDb(fixture.SecondConnectionString);
        Assert.Empty(await firstDb.Categories.ToListAsync());
        Assert.Empty(await secondDb.Categories.ToListAsync());
    }

    private static TenantDbContext CreateTenantDb(string connectionString) =>
        new(new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connectionString).Options);

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
    }

    private sealed class MutableCatalog(IReadOnlyList<TenantFinanceMigrationTarget> targets)
        : ITenantProvisioningCatalog
    {
        public IReadOnlyList<TenantFinanceMigrationTarget> Targets { get; set; } = targets;

        public Task<TenantProvisioningTarget?> GetAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantProvisioningTarget?>(null);

        public Task<IReadOnlyList<TenantFinanceMigrationTarget>> ListReadyAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(Targets);
    }

    private sealed class ChangingCatalog(
        IReadOnlyList<TenantFinanceMigrationTarget> first,
        IReadOnlyList<TenantFinanceMigrationTarget> subsequent) : ITenantProvisioningCatalog
    {
        private int _calls;

        public Task<TenantProvisioningTarget?> GetAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantProvisioningTarget?>(null);

        public Task<IReadOnlyList<TenantFinanceMigrationTarget>> ListReadyAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Interlocked.Increment(ref _calls) == 1 ? first : subsequent);
    }
}
