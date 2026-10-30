using FinancasSaaS.Application.Finance.InstallmentPurchases;
using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Application.Abstractions;

public interface IInstallmentPurchaseStore
{
    Task<InstallmentPurchaseRecord> CreateAsync(
        InstallmentPurchase purchase,
        InstallmentPurchaseAuditEvent auditEvent,
        CancellationToken cancellationToken = default);
    Task<InstallmentPurchaseRecord?> GetAsync(Guid purchaseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstallmentPurchaseRecord>> ListAsync(CancellationToken cancellationToken = default);
    Task<InstallmentPurchaseRecord?> RealizeAsync(
        Guid purchaseId,
        int installmentNumber,
        DateOnly effectiveDate,
        long expectedVersion,
        DateTimeOffset occurredAt,
        Guid actorId,
        CancellationToken cancellationToken = default);
    Task<InstallmentPurchaseRecord?> EditAsync(
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
        CancellationToken cancellationToken = default);
    Task<InstallmentPurchaseRecord?> CancelAsync(
        Guid purchaseId,
        DateOnly operationDate,
        string reason,
        long expectedVersion,
        DateTimeOffset occurredAt,
        Guid actorId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InstallmentPurchaseAuditEvent>> GetHistoryAsync(
        Guid purchaseId,
        CancellationToken cancellationToken = default);
}
