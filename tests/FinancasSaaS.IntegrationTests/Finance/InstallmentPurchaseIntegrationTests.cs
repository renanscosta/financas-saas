using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Finance.Infrastructure.Categories;
using FinancasSaaS.Finance.Infrastructure.InstallmentPurchases;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Finance;

[Collection("Tenant database integration")]
public sealed class InstallmentPurchaseIntegrationTests(TenantDatabaseFixture fixture)
{
    [Fact]
    public async Task CompraParceladaAuditoriaEMutacoesSaoAtomicasEIsoladasPorTenant()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        await TenantDatabaseFixture.ResetTenantAsync(fixture.SecondConnectionString);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var currentUser = new MutableCurrentUserContext(userA);
        var contextFactory = new TenantFinanceContextFactory(
            currentUser,
            new TestTenantConnectionResolver(new Dictionary<Guid, string>
            {
                [userA] = fixture.FirstConnectionString,
                [userB] = fixture.SecondConnectionString
            }));
        var categoryStore = new CategoryStore(contextFactory);
        var categoryA = await categoryStore.CreateAsync($"categoria-a-{Guid.NewGuid():N}");
        currentUser.UserId = userB;
        var categoryB = await categoryStore.CreateAsync($"categoria-b-{Guid.NewGuid():N}");
        currentUser.UserId = userA;
        var categoryStoreA = new CategoryStore(contextFactory);
        var replacementCategory = await categoryStoreA.CreateAsync($"categoria-a2-{Guid.NewGuid():N}");
        var store = new InstallmentPurchaseStore(contextFactory);
        var actorId = Guid.NewGuid();
        var purchase = CreatePurchase(categoryA.Id, 100m, 3);
        var creationAudit = purchase.CreateInitialAuditEvent(Utc(2026, 1, 1), actorId);
        var created = await store.CreateAsync(purchase, creationAudit);

        Assert.Equal([33.34m, 33.33m, 33.33m], created.Installments.Select(item => item.Installment.Amount));
        Assert.Equal(3, created.Installments.Count);
        Assert.Equal(creationAudit.Id, Assert.Single(await store.GetHistoryAsync(purchase.Id)).Id);

        currentUser.UserId = userB;
        Assert.Null(await store.GetAsync(purchase.Id));
        Assert.Empty(await store.ListAsync());
        Assert.Empty(await store.GetHistoryAsync(purchase.Id));
        Assert.Null(await store.RealizeAsync(
            purchase.Id,
            1,
            new DateOnly(2026, 1, 31),
            created.Purchase.Version,
            Utc(2026, 1, 31),
            actorId));
        Assert.Null(await store.EditAsync(
            purchase.Id,
            "Compra adulterada",
            categoryB.Id,
            999m,
            null,
            new DateOnly(2026, 1, 1),
            "Tentativa externa",
            created.Purchase.Version,
            Utc(2026, 1, 1),
            actorId));
        Assert.Null(await store.CancelAsync(
            purchase.Id,
            new DateOnly(2026, 1, 1),
            "Tentativa externa",
            created.Purchase.Version,
            Utc(2026, 1, 1),
            actorId));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            var foreignPurchase = CreatePurchase(categoryA.Id, 10m, 2);
            await store.CreateAsync(
                foreignPurchase,
                foreignPurchase.CreateInitialAuditEvent(Utc(2026, 1, 1), actorId));
        });
        Assert.NotNull(await categoryStore.GetAsync(categoryB.Id));

        currentUser.UserId = userA;
        var realized = await store.RealizeAsync(
            purchase.Id,
            1,
            new DateOnly(2026, 1, 31),
            created.Purchase.Version,
            Utc(2026, 1, 31),
            actorId);
        Assert.NotNull(realized);
        Assert.Null(await store.RealizeAsync(
            purchase.Id,
            1,
            new DateOnly(2026, 2, 1),
            created.Purchase.Version,
            Utc(2026, 2, 1),
            actorId));

        var edited = await store.EditAsync(
            purchase.Id,
            "Compra atualizada",
            replacementCategory.Id,
            110m,
            null,
            new DateOnly(2026, 1, 1),
            "Atualização do compromisso",
            realized!.Purchase.Version,
            Utc(2026, 2, 2),
            actorId);
        Assert.NotNull(edited);
        Assert.Equal(33.34m, edited.Installments[0].Installment.Amount);
        Assert.Equal(ManualTransactionStatus.Realized, edited.Installments[0].Installment.Status);
        Assert.All(edited.Installments.Skip(1), item =>
        {
            Assert.Equal(38.33m, item.Installment.Amount);
            Assert.Equal(replacementCategory.Id, item.Installment.CategoryId);
            Assert.Equal("Compra atualizada", item.Installment.Description);
        });

        var stale = await store.CancelAsync(
            purchase.Id,
            new DateOnly(2026, 1, 1),
            "Versão obsoleta",
            realized.Purchase.Version,
            Utc(2026, 2, 3),
            actorId);
        Assert.Null(stale);

        var cancelled = await store.CancelAsync(
            purchase.Id,
            new DateOnly(2026, 1, 1),
            "Compra descontinuada",
            edited!.Purchase.Version,
            Utc(2026, 2, 4),
            actorId);
        Assert.NotNull(cancelled);
        Assert.Equal(ManualTransactionStatus.Realized, cancelled.Installments[0].Installment.Status);
        Assert.All(cancelled.Installments.Skip(1), item =>
            Assert.Equal(ManualTransactionStatus.Cancelled, item.Installment.Status));
        Assert.Equal(110m, cancelled.Installments.Sum(item => item.Installment.Amount));

        var history = await store.GetHistoryAsync(purchase.Id);
        Assert.Equal(["Creation", "Realization", "Edit", "Cancellation"], history.Select(item => item.EventType));
        Assert.All(history, item => Assert.Equal(actorId, item.ActorId));
        Assert.Equal("Atualização do compromisso", history[2].Reason);
        Assert.Equal("Compra descontinuada", history[3].Reason);
    }

    [Fact]
    public async Task FalhaAoGravarAuditoriaReverteCompraEParcelasInteiras()
    {
        await TenantDatabaseFixture.ResetTenantAsync(fixture.FirstConnectionString);
        var userId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var contextFactory = new TenantFinanceContextFactory(
            new MutableCurrentUserContext(userId),
            new TestTenantConnectionResolver(new Dictionary<Guid, string>
            {
                [userId] = fixture.FirstConnectionString
            }));
        var category = await new CategoryStore(contextFactory).CreateAsync($"atomic-{Guid.NewGuid():N}");
        var store = new InstallmentPurchaseStore(contextFactory);
        var firstPurchase = CreatePurchase(category.Id, 60m, 3);
        var firstAudit = firstPurchase.CreateInitialAuditEvent(Utc(2026, 1, 1), actorId);
        await store.CreateAsync(firstPurchase, firstAudit);

        var rejectedPurchase = CreatePurchase(category.Id, 90m, 3);
        var duplicateAudit = firstAudit with { PurchaseId = rejectedPurchase.Id };
        await Assert.ThrowsAsync<DbUpdateException>(() => store.CreateAsync(rejectedPurchase, duplicateAudit));

        await using var db = CreateContext(fixture.FirstConnectionString);
        Assert.Equal(1, await db.InstallmentPurchases.CountAsync());
        Assert.Equal(3, await db.ManualTransactions.CountAsync());
        Assert.Equal(1, await db.InstallmentPurchaseAuditEvents.CountAsync());
    }

    private static InstallmentPurchase CreatePurchase(Guid categoryId, decimal total, int count) =>
        InstallmentPurchase.Create(
            Guid.NewGuid(),
            "Compra de teste",
            categoryId,
            total,
            count,
            new DateOnly(2026, 2, 28),
            Utc(2026, 1, 1));

    private static DateTimeOffset Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private static TenantDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<TenantDbContext>().UseNpgsql(connectionString).Options);

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
}
