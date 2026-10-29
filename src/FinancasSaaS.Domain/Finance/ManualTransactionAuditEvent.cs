namespace FinancasSaaS.Domain.Finance;

public sealed record ManualTransactionChange(string Field, string? PreviousValue, string? NewValue);

public sealed record ManualTransactionAuditEvent(
    Guid Id,
    Guid TransactionId,
    string EventType,
    IReadOnlyList<ManualTransactionChange> Changes,
    DateTimeOffset OccurredAt,
    Guid ActorId,
    string Reason);
