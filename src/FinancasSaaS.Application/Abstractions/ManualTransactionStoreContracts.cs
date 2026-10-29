using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Application.Finance.ManualTransactions;

namespace FinancasSaaS.Application.Abstractions;

public sealed record ManualTransactionRecord(
    ManualTransaction Transaction,
    long Version,
    string CategoryName);

public interface IManualTransactionStore
{
    Task<ManualTransactionRecord> CreateAsync(ManualTransaction transaction, CancellationToken cancellationToken = default);
    Task<ManualTransactionRecord?> GetAsync(Guid transactionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManualTransactionRecord>> ListAsync(ManualTransactionFilter filter, CancellationToken cancellationToken = default);
    Task<ManualTransactionRecord?> RealizeAsync(Guid transactionId, DateOnly effectiveDate, long expectedVersion, CancellationToken cancellationToken = default);
    Task<ManualTransactionRecord?> CorrectAsync(
        Guid transactionId,
        ManualTransactionCorrection correction,
        long expectedVersion,
        DateTimeOffset occurredAt,
        Guid actorId,
        CancellationToken cancellationToken = default);
    Task<ManualTransactionRecord?> CancelAsync(
        Guid transactionId,
        string reason,
        long expectedVersion,
        DateTimeOffset occurredAt,
        Guid actorId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ManualTransactionAuditEvent>> GetHistoryAsync(Guid transactionId, CancellationToken cancellationToken = default);
}
