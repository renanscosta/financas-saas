using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Finance.Infrastructure.Persistence.Entities;

public sealed class ManualTransactionEntity
{
    public Guid Id { get; set; }
    public ManualTransactionType Type { get; set; }
    public required string Description { get; set; }
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; }
    public DateOnly CompetenceDate { get; set; }
    public DateOnly ExpectedDate { get; set; }
    public ManualTransactionStatus Status { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public ManualTransactionStatus? StatusBeforeCancellation { get; set; }
    public long Version { get; set; }
    public Guid? InstallmentPurchaseId { get; set; }
    public int? InstallmentNumber { get; set; }
    public Category? Category { get; set; }
    public InstallmentPurchaseEntity? InstallmentPurchase { get; set; }
    public ICollection<ManualTransactionAuditEventEntity> AuditEvents { get; set; } = [];
}
