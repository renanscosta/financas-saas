using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance.Categories;
using FinancasSaaS.Application.Finance.ManualTransactions;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Finance.Infrastructure.Categories;
using FinancasSaaS.Finance.Infrastructure.ManualTransactions;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Finance.Infrastructure.Persistence.Entities;
using FinancasSaaS.Finance.Infrastructure.Provisioning;
using FinancasSaaS.IntegrationTests.Infrastructure;
using FinancasSaaS.Platform.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;
using DomainCategory = FinancasSaaS.Domain.Finance.Category;
using EntityCategory = FinancasSaaS.Finance.Infrastructure.Persistence.Entities.Category;

namespace FinancasSaaS.IntegrationTests.Finance;

[Collection("Tenant database integration")]
public sealed class ManualTransactionIntegrationTests(TenantDatabaseFixture fixture)
{
    [Fact]
    public async Task LancamentosEHistoricoFicamIsoladosPorTenantERecusamVersaoObsoleta()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await TenantDatabaseFixture.ResetTenantAsync(fixture.SecondConnectionString);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var currentUser = new MutableCurrentUserContext(userA);
        var resolver = new TestTenantConnectionResolver(new Dictionary<Guid, string>
        {
            [userA] = fixture.FirstConnectionString,
            [userB] = fixture.SecondConnectionString
        });
        var contextFactory = new TenantFinanceContextFactory(currentUser, resolver);
        var categoryStore = new CategoryStore(contextFactory);
        var categoryA = await categoryStore.CreateAsync($"categoria-a-{Guid.NewGuid():N}");
        currentUser.UserId = userB;
        var categoryB = await categoryStore.CreateAsync($"categoria-b-{Guid.NewGuid():N}");
        currentUser.UserId = userA;
        var transactionStore = new ManualTransactionStore(contextFactory);
        var actorId = Guid.NewGuid();
        var transaction = ManualTransaction.Create(
            Guid.NewGuid(),
            ManualTransactionType.Expense,
            "Mercado",
            12.34m,
            categoryA.Id,
            new DateOnly(2026, 10, 28),
            new DateOnly(2026, 10, 29));
        var record = await transactionStore.CreateAsync(transaction);

        currentUser.UserId = userB;
        Assert.Null(await transactionStore.GetAsync(record.Transaction.Id));
        Assert.Null(await transactionStore.RealizeAsync(record.Transaction.Id, new DateOnly(2026, 10, 30), record.Version));
        Assert.Null(await transactionStore.CorrectAsync(
            record.Transaction.Id,
            new ManualTransactionCorrection(null, null, 99.99m, null, null, null, "tentativa externa"),
            record.Version,
            DateTimeOffset.UtcNow,
            actorId));
        Assert.Null(await transactionStore.CancelAsync(
            record.Transaction.Id,
            "tentativa externa",
            record.Version,
            DateTimeOffset.UtcNow,
            actorId));
        await Assert.ThrowsAsync<InvalidOperationException>(() => transactionStore.CreateAsync(
            ManualTransaction.Create(
                Guid.NewGuid(),
                ManualTransactionType.Expense,
                "Categoria alheia",
                1m,
                categoryA.Id,
                new DateOnly(2026, 10, 28),
                new DateOnly(2026, 10, 29))));
        Assert.Null(await categoryStore.GetAsync(categoryA.Id));

        currentUser.UserId = userA;
        var realized = await transactionStore.RealizeAsync(record.Transaction.Id, new DateOnly(2026, 10, 30), record.Version);
        Assert.NotNull(realized);
        Assert.Null(await transactionStore.RealizeAsync(record.Transaction.Id, new DateOnly(2026, 10, 31), record.Version));

        var correction = new ManualTransactionCorrection(
            null,
            null,
            13.45m,
            null,
            null,
            null,
            "Valor revisado.");
        var correctionOccurredAt = new DateTimeOffset(2026, 10, 28, 12, 30, 0, TimeSpan.Zero);
        var corrected = await transactionStore.CorrectAsync(
            record.Transaction.Id,
            correction,
            realized!.Version,
            correctionOccurredAt,
            actorId);
        Assert.NotNull(corrected);
        Assert.Equal(ManualTransactionStatus.Realized, corrected.Transaction.Status);
        Assert.Equal(new DateOnly(2026, 10, 30), corrected.Transaction.EffectiveDate);
        Assert.Null(await transactionStore.CorrectAsync(
            record.Transaction.Id,
            correction,
            realized.Version,
            DateTimeOffset.UtcNow,
            actorId));

        var history = await transactionStore.GetHistoryAsync(record.Transaction.Id);
        var audit = Assert.Single(history);
        Assert.Equal(actorId, audit.ActorId);
        Assert.Equal("Correction", audit.EventType);
        Assert.Equal(correctionOccurredAt, audit.OccurredAt);
        Assert.Equal("Valor revisado.", audit.Reason);
        Assert.Equal("12.34", Assert.Single(audit.Changes).PreviousValue);
        Assert.Equal("13.45", Assert.Single(audit.Changes).NewValue);

        currentUser.UserId = userB;
        Assert.Empty(await transactionStore.GetHistoryAsync(record.Transaction.Id));
        Assert.NotNull(await categoryStore.GetAsync(categoryB.Id));
    }

    [Fact]
    public async Task FiltrosIncluemLimitesUsamDataDoEstadoECancelamentoPreservaHistorico()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var currentUser = new MutableCurrentUserContext(userId);
        var contextFactory = new TenantFinanceContextFactory(
            currentUser,
            new TestTenantConnectionResolver(new Dictionary<Guid, string> { [userId] = fixture.FirstConnectionString }));
        var categories = new CategoryStore(contextFactory);
        var category = await categories.CreateAsync($"filtro-{Guid.NewGuid():N}");
        var store = new ManualTransactionStore(contextFactory);

        var planned = await CreateTransactionAsync(store, category.Id, expectedDate: new DateOnly(2026, 10, 1));
        var realized = await CreateTransactionAsync(store, category.Id, expectedDate: new DateOnly(2026, 10, 2));
        var cancelled = await CreateTransactionAsync(store, category.Id, expectedDate: new DateOnly(2026, 10, 3));
        var outsideRange = await CreateTransactionAsync(store, category.Id, expectedDate: new DateOnly(2026, 11, 1));
        var realizedRecord = await store.RealizeAsync(
            realized.Transaction.Id,
            new DateOnly(2026, 10, 10),
            realized.Version);
        Assert.NotNull(realizedRecord);
        var cancelledRecord = await store.CancelAsync(
            cancelled.Transaction.Id,
            "Registro removido do planejamento.",
            cancelled.Version,
            DateTimeOffset.UtcNow,
            actorId);
        Assert.NotNull(cancelledRecord);
        Assert.Null(await store.CancelAsync(
            cancelled.Transaction.Id,
            "Tentativa com versão obsoleta.",
            cancelled.Version,
            DateTimeOffset.UtcNow,
            actorId));

        var realizedCancellation = await CreateTransactionAsync(store, category.Id, expectedDate: new DateOnly(2026, 10, 4));
        var realizedVersion = await store.RealizeAsync(
            realizedCancellation.Transaction.Id,
            new DateOnly(2026, 10, 9),
            realizedCancellation.Version);
        Assert.NotNull(realizedVersion);
        var cancelledRealized = await store.CancelAsync(
            realizedCancellation.Transaction.Id,
            "Pagamento estornado.",
            realizedVersion!.Version,
            DateTimeOffset.UtcNow,
            actorId);
        Assert.NotNull(cancelledRealized);

        var filter = new ManualTransactionFilter(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 10),
            ManualTransactionType.Expense,
            category.Id);
        var result = await store.ListAsync(filter);

        Assert.Contains(result, item => item.Transaction.Id == planned.Transaction.Id);
        Assert.Contains(result, item => item.Transaction.Id == realized.Transaction.Id);
        Assert.Contains(result, item => item.Transaction.Id == cancelled.Transaction.Id);
        Assert.Contains(result, item => item.Transaction.Id == realizedCancellation.Transaction.Id);
        Assert.DoesNotContain(result, item => item.Transaction.Id == outsideRange.Transaction.Id);
        Assert.DoesNotContain(result, item => item.Transaction.Id == cancelled.Transaction.Id &&
            item.Transaction.Status != ManualTransactionStatus.Cancelled);

        var plannedOnly = await store.ListAsync(filter with { Status = ManualTransactionStatus.Planned });
        var realizedOnly = await store.ListAsync(filter with { Status = ManualTransactionStatus.Realized });
        var cancelledOnly = await store.ListAsync(filter with { Status = ManualTransactionStatus.Cancelled });
        Assert.Equal([planned.Transaction.Id], plannedOnly.Select(item => item.Transaction.Id));
        Assert.Equal([realized.Transaction.Id], realizedOnly.Select(item => item.Transaction.Id));
        Assert.Equal(
            new[] { cancelled.Transaction.Id, realizedCancellation.Transaction.Id }.Order(),
            cancelledOnly.Select(item => item.Transaction.Id).Order());

        var cancelledHistory = await store.GetHistoryAsync(cancelled.Transaction.Id);
        var cancellation = Assert.Single(cancelledHistory);
        Assert.Equal("Planned", Assert.Single(cancellation.Changes).PreviousValue);
        Assert.Equal("Cancelled", Assert.Single(cancellation.Changes).NewValue);
        Assert.Equal(actorId, cancellation.ActorId);
        var realizedCancellationHistory = await store.GetHistoryAsync(realizedCancellation.Transaction.Id);
        Assert.Single(realizedCancellationHistory);
        Assert.Equal("Realized", Assert.Single(realizedCancellationHistory[^1].Changes).PreviousValue);
    }

    [Fact]
    public async Task CategoriasRecusamDuplicataNormalizadaAssociacaoEExclusaoDaUltima()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.SecondConnectionString);
        var userId = Guid.NewGuid();
        var contextFactory = new TenantFinanceContextFactory(
            new MutableCurrentUserContext(userId),
            new TestTenantConnectionResolver(new Dictionary<Guid, string> { [userId] = fixture.SecondConnectionString }));
        var categories = new CategoryStore(contextFactory);
        var category = await categories.CreateAsync($"  categoria-{Guid.NewGuid():N}  ");

        await Assert.ThrowsAsync<CategoryNameConflictException>(() =>
            categories.CreateAsync(category.Name.ToUpperInvariant()));

        var transactionStore = new ManualTransactionStore(contextFactory);
        await CreateTransactionAsync(transactionStore, category.Id);
        Assert.Equal(CategoryDeleteResult.InUse, await categories.DeleteAsync(category.Id));

        var removable = await categories.CreateAsync($"removivel-{Guid.NewGuid():N}");
        Assert.Equal(CategoryDeleteResult.Deleted, await categories.DeleteAsync(removable.Id));

        await TenantDatabaseFixture.ResetTenantAsync(fixture.SecondConnectionString);
        var onlyCategory = await categories.CreateAsync($"unica-{Guid.NewGuid():N}");
        Assert.Equal(CategoryDeleteResult.LastCategory, await categories.DeleteAsync(onlyCategory.Id));
    }

    [Fact]
    public async Task CriacoesConcorrentesDeCategoriaNormalizadaMantemUmaUnica()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.SecondConnectionString);
        var userId = Guid.NewGuid();
        var contextFactory = new TenantFinanceContextFactory(
            new MutableCurrentUserContext(userId),
            new TestTenantConnectionResolver(new Dictionary<Guid, string> { [userId] = fixture.SecondConnectionString }));
        var categories = new CategoryStore(contextFactory);
        var name = $"concorrente-{Guid.NewGuid():N}";

        var results = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(async index =>
            {
                try
                {
                    await categories.CreateAsync(index == 0 ? name : $" {name.ToUpperInvariant()} ");
                    return true;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }));

        Assert.Single(results, created => created);
        Assert.Single(results, created => !created);
        Assert.Single(await categories.ListAsync(), item =>
            item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CategoriasInativasNaoSaoListadasNemAceitasEmNovosLancamentos()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.SecondConnectionString);
        var userId = Guid.NewGuid();
        var contextFactory = new TenantFinanceContextFactory(
            new MutableCurrentUserContext(userId),
            new TestTenantConnectionResolver(new Dictionary<Guid, string> { [userId] = fixture.SecondConnectionString }));
        var categories = new CategoryStore(contextFactory);
        var inactive = await categories.CreateAsync($"inativa-{Guid.NewGuid():N}");

        await using (var db = new TenantDbContext(new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(fixture.SecondConnectionString)
            .Options))
        {
            var entity = await db.Categories.SingleAsync(item => item.Id == inactive.Id);
            entity.IsActive = false;
            await db.SaveChangesAsync();
        }

        Assert.DoesNotContain(await categories.ListAsync(), item => item.Id == inactive.Id);

        var transactions = new ManualTransactionStore(contextFactory);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateTransactionAsync(transactions, inactive.Id));
    }

    [Fact]
    public async Task RunnerRecusaInventarioDivergenteAntesDeConectarAosBancos()
    {
        var userId = Guid.NewGuid();
        var catalog = new TestMigrationCatalog(
        [
            new TenantFinanceMigrationTarget(userId, "tenant", fixture.FirstConnectionString)
        ]);
        var runner = new TenantFinanceMigrationRunner(catalog, new TestClock());

        await Assert.ThrowsAsync<TenantFinanceMigrationException>(() =>
            runner.MigrateAsync(new TenantFinanceInventory(DateTimeOffset.UtcNow, [])));
    }

    [Fact]
    public async Task SeedReconciliaCategoriaEquivalenteEPermaneceIdempotente()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(fixture.FirstConnectionString)
            .Options;
        var existingId = Guid.NewGuid();
        await using (var db = new TenantDbContext(options))
        {
            db.Categories.Add(new EntityCategory { Id = existingId, Name = "lAnChEs", IsActive = false });
            await db.SaveChangesAsync();
        }

        var target = new TenantProvisioningTarget(
            new Npgsql.NpgsqlConnectionStringBuilder(fixture.FirstConnectionString).Database
                ?? throw new InvalidOperationException("The test database name is missing."),
            fixture.FirstConnectionString);
        var provisioner = new PostgresTenantProvisioner(
            new TestProvisioningCatalog(target),
            new TestProvisioningStateStore(),
            new TestClock());
        await provisioner.ProvisionAsync(Guid.NewGuid());
        await provisioner.ProvisionAsync(Guid.NewGuid());

        await using var verify = new TenantDbContext(options);
        var categories = await verify.Categories.AsNoTracking().ToListAsync();
        Assert.Equal(3, categories.Count);
        Assert.Single(categories, category => DomainCategory.NormalizeName(category.Name) == DomainCategory.NormalizeName("Lanches"));
        Assert.Contains(categories, category =>
            category.Id == existingId && category.Name == "lAnChEs" && category.IsActive);
        Assert.Contains(categories, category => category.Name == "Mercado");
        Assert.Contains(categories, category => category.Name == "Pessoal");
    }

    [Fact]
    public async Task RenomearCategoriaAtualizaNomeExibidoNosLancamentosExistentes()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        var userId = Guid.NewGuid();
        var contextFactory = new TenantFinanceContextFactory(
            new MutableCurrentUserContext(userId),
            new TestTenantConnectionResolver(new Dictionary<Guid, string>
            {
                [userId] = fixture.FirstConnectionString
            }));
        var categoryStore = new CategoryStore(contextFactory);
        var category = await categoryStore.CreateAsync($"categoria-{Guid.NewGuid():N}");
        var transactionStore = new ManualTransactionStore(contextFactory);
        var transaction = await CreateTransactionAsync(transactionStore, category.Id);

        await categoryStore.RenameAsync(category.Id, "Nome atualizado");

        var updated = await transactionStore.GetAsync(transaction.Transaction.Id);

        Assert.NotNull(updated);
        Assert.Equal("Nome atualizado", updated.CategoryName);
    }

    [Fact]
    public async Task MigrationRecusaNomesLegadosEquivalentesSemAlterarCategorias()
    {
        var (tenantId, connectionString) = await fixture.CreateTemporaryTenantDatabaseAsync();
        try
        {
            var options = new DbContextOptionsBuilder<TenantDbContext>()
                .UseNpgsql(connectionString)
                .Options;
            await using (var db = new TenantDbContext(options))
            {
                await db.Database.MigrateAsync("20260929202021_TenantInitial");
                await db.Database.ExecuteSqlRawAsync(
                    """INSERT INTO "Categories" ("Id", "Name", "IsActive") VALUES ({0}, {1}, TRUE), ({2}, {3}, TRUE)""",
                    Guid.NewGuid(),
                    "Mercado",
                    Guid.NewGuid(),
                    " mercado ");

                await Assert.ThrowsAsync<PostgresException>(() => db.Database.MigrateAsync());
                Assert.Equal(2, await db.Categories.CountAsync());
                Assert.Equal(
                    ["20260929202021_TenantInitial"],
                    await db.Database.GetAppliedMigrationsAsync());
            }
        }
        finally
        {
            await fixture.DeleteTemporaryTenantDatabaseAsync(tenantId);
        }
    }

    private static Task<ManualTransactionRecord> CreateTransactionAsync(
        ManualTransactionStore store,
        Guid categoryId,
        DateOnly? expectedDate = null) =>
        store.CreateAsync(ManualTransaction.Create(
            Guid.NewGuid(),
            ManualTransactionType.Expense,
            $"Compra-{Guid.NewGuid():N}",
            25.50m,
            categoryId,
            new DateOnly(2026, 10, 1),
            expectedDate ?? new DateOnly(2026, 10, 2)));

    private sealed class MutableCurrentUserContext(Guid userId) : ICurrentUserContext
    {
        public Guid UserId { get; set; } = userId;

        public Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(UserId);

        public Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(UserId);
    }

    private sealed class TestTenantConnectionResolver(IReadOnlyDictionary<Guid, string> connections)
        : ITenantConnectionResolver
    {
        public Task<string?> ResolveAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(connections.GetValueOrDefault(userId));

        public Task<Guid?> ResolveTenantIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(connections.ContainsKey(userId) ? userId : null);
    }

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class TestMigrationCatalog(IReadOnlyList<TenantFinanceMigrationTarget> targets)
        : ITenantProvisioningCatalog
    {
        public Task<TenantProvisioningTarget?> GetAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantProvisioningTarget?>(null);

        public Task<IReadOnlyList<TenantFinanceMigrationTarget>> ListReadyAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(targets);
    }

    private sealed class TestProvisioningCatalog(TenantProvisioningTarget target) : ITenantProvisioningCatalog
    {
        public Task<TenantProvisioningTarget?> GetAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) => Task.FromResult<TenantProvisioningTarget?>(target);

        public Task<IReadOnlyList<TenantFinanceMigrationTarget>> ListReadyAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TenantFinanceMigrationTarget>>([]);
    }

    private sealed class TestProvisioningStateStore : ITenantProvisioningStateStore
    {
        public Task<bool> BeginAsync(
            Guid tenantId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task MarkReadyAsync(
            Guid tenantId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task MarkFailedAsync(
            Guid tenantId,
            DateTimeOffset now,
            string failure,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
