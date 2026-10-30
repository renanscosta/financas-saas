namespace FinancasSaaS.Finance.Infrastructure.Persistence.Entities;

public sealed class InstallmentPurchaseEntity
{
    public Guid Id { get; set; }
    public required string Description { get; set; }
    public Guid CategoryId { get; set; }
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public DateOnly OriginalFirstDueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public long Version { get; set; }
    public Category? Category { get; set; }
    public ICollection<ManualTransactionEntity> Installments { get; set; } = [];
    public ICollection<InstallmentPurchaseAuditEventEntity> AuditEvents { get; set; } = [];
}
