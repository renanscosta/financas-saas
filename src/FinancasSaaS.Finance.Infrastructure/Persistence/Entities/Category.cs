namespace FinancasSaaS.Finance.Infrastructure.Persistence.Entities;

public sealed class Category
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; }
}
