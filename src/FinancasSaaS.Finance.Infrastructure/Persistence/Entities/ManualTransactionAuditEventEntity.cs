namespace FinancasSaaS.Finance.Infrastructure.Persistence.Entities;

public sealed class ManualTransactionAuditEventEntity
{
    public Guid Id { get; set; }
    public Guid TransactionId { get; set; }
    public required string EventType { get; set; }
    public required string ChangesJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid ActorId { get; set; }
    public required string Reason { get; set; }
    public ManualTransactionEntity? Transaction { get; set; }
}
