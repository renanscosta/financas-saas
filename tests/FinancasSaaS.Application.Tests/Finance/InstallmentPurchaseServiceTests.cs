using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance.InstallmentPurchases;
using FinancasSaaS.Domain.Finance;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinancasSaaS.Application.Tests.Finance;

public sealed class InstallmentPurchaseServiceTests
{
    [Fact]
    public async Task CriacaoDerivaAtorEInstanteDoContextoServidor()
    {
        var actorId = Guid.NewGuid();
        var store = new RecordingStore();
        var service = new InstallmentPurchaseService(
            store,
            new FixedUserContext(actorId, Guid.NewGuid()),
            new FixedClock(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<InstallmentPurchaseService>.Instance);

        var result = await service.CreateAsync(
            "Compra de teste",
            Guid.NewGuid(),
            100m,
            3,
            new DateOnly(2026, 1, 31));

        Assert.NotNull(store.Purchase);
        Assert.Equal(actorId, store.AuditEvent!.ActorId);
        Assert.Equal(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero), store.AuditEvent.OccurredAt);
        Assert.Equal(100m, result.Installments.Sum(item => item.Installment.Amount));
        Assert.Equal("Creation", store.AuditEvent.EventType);
    }

    [Fact]
    public async Task RecusaCriacaoSemIdentidadeAutenticadaAntesDePersistir()
    {
        var store = new RecordingStore();
        var service = new InstallmentPurchaseService(
            store,
            new FixedUserContext(null, Guid.NewGuid()),
            new FixedClock(DateTimeOffset.UnixEpoch),
            NullLogger<InstallmentPurchaseService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            "Compra",
            Guid.NewGuid(),
            10m,
            2,
            new DateOnly(2026, 1, 1)));

        Assert.Null(store.Purchase);
    }

    private sealed class FixedUserContext(Guid? actorId, Guid? tenantId) : ICurrentUserContext
    {
        public Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(actorId);

        public Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(tenantId);
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow { get; } = now;
    }

    private sealed class RecordingStore : IInstallmentPurchaseStore
    {
        public InstallmentPurchase? Purchase { get; private set; }
        public InstallmentPurchaseAuditEvent? AuditEvent { get; private set; }
        public Task<InstallmentPurchaseRecord> CreateAsync(
            InstallmentPurchase purchase,
            InstallmentPurchaseAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            Purchase = purchase;
            AuditEvent = auditEvent;
            var installments = purchase.Installments
                .Select(item => new InstallmentPurchaseInstallmentRecord(item, "Categoria"))
                .ToArray();
            return Task.FromResult(new InstallmentPurchaseRecord(purchase, "Categoria", installments));
        }

        public Task<InstallmentPurchaseRecord?> GetAsync(Guid purchaseId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<InstallmentPurchaseRecord>> ListAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<InstallmentPurchaseRecord?> RealizeAsync(
            Guid purchaseId,
            int installmentNumber,
            DateOnly effectiveDate,
            long expectedVersion,
            DateTimeOffset occurredAt,
            Guid actorId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<InstallmentPurchaseRecord?> EditAsync(
            Guid purchaseId,
            string description,
            Guid categoryId,
            decimal totalAmount,
            DateOnly? firstEligibleDueDate,
            DateOnly operationDate,
            string reason,
            long expectedVersion,
            DateTimeOffset occurredAt,
            Guid actorId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<InstallmentPurchaseRecord?> CancelAsync(
            Guid purchaseId,
            DateOnly operationDate,
            string reason,
            long expectedVersion,
            DateTimeOffset occurredAt,
            Guid actorId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<InstallmentPurchaseAuditEvent>> GetHistoryAsync(
            Guid purchaseId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
