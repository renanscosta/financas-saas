namespace FinancasSaaS.Domain.Finance;

public sealed record InstallmentPurchaseAuditChange(
    int? InstallmentNumber,
    string Field,
    string? PreviousValue,
    string? NewValue);

public sealed record InstallmentPurchaseAuditEvent(
    Guid Id,
    Guid PurchaseId,
    string EventType,
    IReadOnlyList<InstallmentPurchaseAuditChange> Changes,
    DateTimeOffset OccurredAt,
    Guid ActorId,
    string Reason);
