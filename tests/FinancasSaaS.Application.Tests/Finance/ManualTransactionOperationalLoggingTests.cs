using System.Diagnostics;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance.ManualTransactions;
using FinancasSaaS.Domain.Finance;
using Microsoft.Extensions.Logging;

namespace FinancasSaaS.Application.Tests.Finance;

public sealed class ManualTransactionOperationalLoggingTests
{
    [Fact]
    public async Task CriacaoRegistraAtorTenantInstanteECorrelationSemPayloadFinanceiro()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var logger = new RecordingLogger<ManualTransactionService>();
        var transaction = ManualTransaction.Create(
            Guid.NewGuid(),
            ManualTransactionType.Expense,
            "Descrição confidencial",
            12.34m,
            Guid.NewGuid(),
            new DateOnly(2026, 10, 28),
            new DateOnly(2026, 10, 29));
        var store = new RecordingTransactionStore(transaction);
        var service = new ManualTransactionService(
            store,
            new FixedUserContext(actorId, tenantId),
            new FixedClock(),
            logger);
        using var activity = new Activity("finance-test").Start();

        await service.CreateAsync(
            transaction.Type,
            transaction.Description,
            transaction.Amount,
            transaction.CategoryId,
            transaction.CompetenceDate,
            transaction.ExpectedDate);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal("transaction.create", entry.Properties["Operation"]);
        Assert.Equal("success", entry.Properties["Result"]);
        Assert.Equal(actorId, entry.Properties["ActorId"]);
        Assert.Equal(tenantId, entry.Properties["TenantId"]);
        Assert.Equal(DateTimeOffset.UnixEpoch, entry.Properties["OccurredAtUtc"]);
        Assert.Equal(activity.TraceId.ToString(), entry.Properties["CorrelationId"]);
        Assert.DoesNotContain("Descrição confidencial", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("12.34", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExcecaoDePersistenciaERegistradaEPropagadaSemPayload()
    {
        var logger = new RecordingLogger<ManualTransactionService>();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var store = new RecordingTransactionStore(null)
        {
            CreateException = new InvalidOperationException("falha técnica")
        };
        var service = new ManualTransactionService(
            store,
            new FixedUserContext(actorId, tenantId),
            new FixedClock(),
            logger);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(
                ManualTransactionType.Income,
                "Valor privado",
                999.99m,
                Guid.NewGuid(),
                new DateOnly(2026, 10, 28),
                new DateOnly(2026, 10, 29)));

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal("transaction.create", entry.Properties["Operation"]);
        Assert.Equal(actorId, entry.Properties["ActorId"]);
        Assert.Equal(tenantId, entry.Properties["TenantId"]);
        Assert.Same(exception, entry.Exception);
        Assert.DoesNotContain("Valor privado", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("999.99", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConflitoNaRealizacaoRegistraResultadoECorrelationId()
    {
        var logger = new RecordingLogger<ManualTransactionService>();
        var activity = new Activity("realize-conflict").Start();
        using (activity)
        {
            var service = new ManualTransactionService(
                new RecordingTransactionStore(null),
                new FixedUserContext(Guid.NewGuid(), Guid.NewGuid()),
                new FixedClock(),
                logger);

            var result = await service.RealizeAsync(Guid.NewGuid(), new DateOnly(2026, 10, 29), 3);

            Assert.Null(result);
            var entry = Assert.Single(logger.Entries);
            Assert.Equal("transaction.realize", entry.Properties["Operation"]);
            Assert.Equal("conflict_or_not_found", entry.Properties["Result"]);
            Assert.Equal(activity.TraceId.ToString(), entry.Properties["CorrelationId"]);
        }
    }

    [Fact]
    public async Task SucessoEFalhaNaRealizacaoRegistramResultadoSemPayload()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var transaction = ManualTransaction.Create(
            Guid.NewGuid(),
            ManualTransactionType.Income,
            "Descrição confidencial",
            12.34m,
            Guid.NewGuid(),
            new DateOnly(2026, 10, 28),
            new DateOnly(2026, 10, 29));
        var logger = new RecordingLogger<ManualTransactionService>();
        var service = new ManualTransactionService(
            new RecordingTransactionStore(transaction)
            {
                RealizeResult = new ManualTransactionRecord(transaction, 2, "category")
            },
            new FixedUserContext(actorId, tenantId),
            new FixedClock(),
            logger);

        await service.RealizeAsync(transaction.Id, new DateOnly(2026, 10, 30), 1);
        var failedStore = new RecordingTransactionStore(transaction)
        {
            RealizeException = new IOException("falha técnica")
        };
        var failedService = new ManualTransactionService(
            failedStore,
            new FixedUserContext(actorId, tenantId),
            new FixedClock(),
            logger);
        var exception = await Assert.ThrowsAsync<IOException>(() =>
            failedService.RealizeAsync(transaction.Id, new DateOnly(2026, 10, 30), 1));

        Assert.Collection(
            logger.Entries,
            success =>
            {
                Assert.Equal(LogLevel.Information, success.Level);
                Assert.Equal("transaction.realize", success.Properties["Operation"]);
                Assert.Equal("success", success.Properties["Result"]);
                Assert.Equal(actorId, success.Properties["ActorId"]);
                Assert.Equal(tenantId, success.Properties["TenantId"]);
                Assert.Equal(DateTimeOffset.UnixEpoch, success.Properties["OccurredAtUtc"]);
                Assert.DoesNotContain("Descrição confidencial", success.Message, StringComparison.Ordinal);
                Assert.DoesNotContain("12.34", success.Message, StringComparison.Ordinal);
            },
            failure =>
            {
                Assert.Equal(LogLevel.Error, failure.Level);
                Assert.Equal("transaction.realize", failure.Properties["Operation"]);
                Assert.Equal(actorId, failure.Properties["ActorId"]);
                Assert.Equal(tenantId, failure.Properties["TenantId"]);
                Assert.Same(exception, failure.Exception);
            });
    }

    [Fact]
    public async Task ExcecoesDeCorrecaoECancelamentoSaoRegistradasEPropagadasSemPayload()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var correctionFailure = new InvalidOperationException("falha técnica de correção");
        var cancellationFailure = new InvalidOperationException("falha técnica de cancelamento");
        var correctionStore = new RecordingTransactionStore(null) { CorrectException = correctionFailure };
        var cancellationStore = new RecordingTransactionStore(null) { CancelException = cancellationFailure };
        var logger = new RecordingLogger<ManualTransactionService>();
        var user = new FixedUserContext(actorId, tenantId);
        var correctionService = new ManualTransactionService(correctionStore, user, new FixedClock(), logger);
        var cancellationService = new ManualTransactionService(cancellationStore, user, new FixedClock(), logger);
        var correction = new ManualTransactionCorrection(null, null, null, null, null, null, "motivo privado");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            correctionService.CorrectAsync(Guid.NewGuid(), correction, 1));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            cancellationService.CancelAsync(Guid.NewGuid(), "motivo privado", 1));

        Assert.Collection(
            logger.Entries,
            correctionEntry =>
            {
                Assert.Equal(LogLevel.Error, correctionEntry.Level);
                Assert.Equal("transaction.correct", correctionEntry.Properties["Operation"]);
                Assert.Equal(actorId, correctionEntry.Properties["ActorId"]);
                Assert.Equal(tenantId, correctionEntry.Properties["TenantId"]);
                Assert.Equal(DateTimeOffset.UnixEpoch, correctionEntry.Properties["OccurredAtUtc"]);
                Assert.Same(correctionFailure, correctionEntry.Exception);
                Assert.DoesNotContain("motivo privado", correctionEntry.Message, StringComparison.Ordinal);
            },
            cancellationEntry =>
            {
                Assert.Equal(LogLevel.Error, cancellationEntry.Level);
                Assert.Equal("transaction.cancel", cancellationEntry.Properties["Operation"]);
                Assert.Equal(actorId, cancellationEntry.Properties["ActorId"]);
                Assert.Equal(tenantId, cancellationEntry.Properties["TenantId"]);
                Assert.Equal(DateTimeOffset.UnixEpoch, cancellationEntry.Properties["OccurredAtUtc"]);
                Assert.Same(cancellationFailure, cancellationEntry.Exception);
                Assert.DoesNotContain("motivo privado", cancellationEntry.Message, StringComparison.Ordinal);
            });
    }

    private sealed class FixedUserContext(Guid actorId, Guid tenantId) : ICurrentUserContext
    {
        public Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(actorId);

        public Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(tenantId);
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
    }

    private sealed class RecordingTransactionStore(ManualTransaction? transaction) : IManualTransactionStore
    {
        public Exception? CreateException { get; init; }
        public Exception? RealizeException { get; init; }
        public Exception? CorrectException { get; init; }
        public Exception? CancelException { get; init; }
        public ManualTransactionRecord? RealizeResult { get; init; }

        public Task<ManualTransactionRecord> CreateAsync(
            ManualTransaction newTransaction,
            CancellationToken cancellationToken = default)
        {
            if (CreateException is not null)
            {
                throw CreateException;
            }

            var value = transaction ?? newTransaction;
            return Task.FromResult(new ManualTransactionRecord(value, 1, "category"));
        }

        public Task<ManualTransactionRecord?> GetAsync(
            Guid transactionId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ManualTransactionRecord>> ListAsync(
            ManualTransactionFilter filter,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<ManualTransactionRecord?> RealizeAsync(
            Guid transactionId,
            DateOnly effectiveDate,
            long expectedVersion,
            CancellationToken cancellationToken = default)
        {
            if (RealizeException is not null)
            {
                throw RealizeException;
            }

            return Task.FromResult(RealizeResult);
        }

        public Task<ManualTransactionRecord?> CorrectAsync(
            Guid transactionId,
            ManualTransactionCorrection correction,
            long expectedVersion,
            DateTimeOffset occurredAt,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (CorrectException is not null)
            {
                throw CorrectException;
            }

            return Task.FromResult<ManualTransactionRecord?>(null);
        }

        public Task<ManualTransactionRecord?> CancelAsync(
            Guid transactionId,
            string reason,
            long expectedVersion,
            DateTimeOffset occurredAt,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            if (CancelException is not null)
            {
                throw CancelException;
            }

            return Task.FromResult<ManualTransactionRecord?>(null);
        }

        public Task<IReadOnlyList<ManualTransactionAuditEvent>> GetHistoryAsync(
            Guid transactionId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(item => item.Key, item => item.Value)
                : [];
            Entries.Add(new LogEntry(logLevel, formatter(state, exception), properties, exception));
        }
    }

    private sealed record LogEntry(
        LogLevel Level,
        string Message,
        IReadOnlyDictionary<string, object?> Properties,
        Exception? Exception);
}
