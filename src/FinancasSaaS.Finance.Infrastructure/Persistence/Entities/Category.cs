namespace FinancasSaaS.Finance.Infrastructure.Persistence.Entities;

public sealed class Category
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string NormalizedName { get; private set; } = string.Empty;
    public bool IsActive { get; set; }
    public ICollection<ManualTransactionEntity> Transactions { get; set; } = [];
}
