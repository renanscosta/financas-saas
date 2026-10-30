namespace FinancasSaaS.Finance.Infrastructure.Persistence.Entities;

public sealed class InstallmentPurchaseAuditEventEntity
{
    public Guid Id { get; set; }
    public Guid PurchaseId { get; set; }
    public required string EventType { get; set; }
    public required string ChangesJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid ActorId { get; set; }
    public required string Reason { get; set; }
    public InstallmentPurchaseEntity? Purchase { get; set; }
}
