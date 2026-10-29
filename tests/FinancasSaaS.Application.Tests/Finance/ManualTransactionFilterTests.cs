using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance;
using FinancasSaaS.Application.Finance.ManualTransactions;
using FinancasSaaS.Domain.Finance;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinancasSaaS.Application.Tests.Finance;

public sealed class ManualTransactionFilterTests
{
    [Fact]
    public async Task ListaRecusaIntervaloInvertido()
    {
        var service = new ManualTransactionService(
            new UnusedTransactionStore(),
            new UnusedCurrentUser(),
            new UnusedClock(),
            NullLogger<ManualTransactionService>.Instance);
        var filter = new ManualTransactionFilter(
            new DateOnly(2026, 10, 29),
            new DateOnly(2026, 10, 28));

        await Assert.ThrowsAsync<ArgumentException>(() => service.ListAsync(filter));
    }

    [Fact]
    public async Task ListaEncaminhaFiltrosCombinadosEPermitePeriodoDeUmDia()
    {
        var store = new UnusedTransactionStore();
        var service = new ManualTransactionService(
            store,
            new UnusedCurrentUser(),
            new UnusedClock(),
            NullLogger<ManualTransactionService>.Instance);
        var date = new DateOnly(2026, 10, 28);
        var filter = new ManualTransactionFilter(
            date,
            date,
            ManualTransactionType.Income,
            Guid.NewGuid(),
            ManualTransactionStatus.Cancelled);

        await service.ListAsync(filter);

        var captured = Assert.IsType<ManualTransactionFilter>(store.RequestedFilter);
        Assert.Same(filter, captured);
        Assert.Equal(date, captured.StartDate);
        Assert.Equal(date, captured.EndDate);
        Assert.Equal(ManualTransactionType.Income, captured.Type);
        Assert.Equal(filter.CategoryId, captured.CategoryId);
        Assert.Equal(ManualTransactionStatus.Cancelled, captured.Status);
    }

    private sealed class UnusedCurrentUser : ICurrentUserContext
    {
        public Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class UnusedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    private sealed class UnusedTransactionStore : IManualTransactionStore
    {
        public ManualTransactionFilter? RequestedFilter { get; private set; }

        public Task<ManualTransactionRecord> CreateAsync(
            ManualTransaction transaction,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<ManualTransactionRecord?> GetAsync(
            Guid transactionId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ManualTransactionRecord>> ListAsync(
            ManualTransactionFilter filter,
            CancellationToken cancellationToken = default)
        {
            RequestedFilter = filter;
            return Task.FromResult<IReadOnlyList<ManualTransactionRecord>>([]);
        }

        public Task<ManualTransactionRecord?> RealizeAsync(
            Guid transactionId,
            DateOnly effectiveDate,
            long expectedVersion,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<ManualTransactionRecord?> CorrectAsync(
            Guid transactionId,
            ManualTransactionCorrection correction,
            long expectedVersion,
            DateTimeOffset occurredAt,
            Guid actorId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<ManualTransactionRecord?> CancelAsync(
            Guid transactionId,
            string reason,
            long expectedVersion,
            DateTimeOffset occurredAt,
            Guid actorId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ManualTransactionAuditEvent>> GetHistoryAsync(
            Guid transactionId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
