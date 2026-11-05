using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Finance.Infrastructure.Persistence.Entities;
using CategoryEntity = FinancasSaaS.Finance.Infrastructure.Persistence.Entities.Category;

namespace FinancasSaaS.IntegrationTests.Infrastructure;

public static class ReportDataSeeder
{
    public static async Task<(Guid CategoryId, Guid PurchaseId)> SeedAsync(TenantDbContext db)
    {
        var categoryId = Guid.NewGuid();
        var purchaseId = Guid.NewGuid();
        db.Categories.Add(new CategoryEntity { Id = categoryId, Name = "Mercado", IsActive = true });
        db.InstallmentPurchases.Add(new InstallmentPurchaseEntity
        {
            Id = purchaseId,
            Description = "Compra parcelada",
            CategoryId = categoryId,
            TotalAmount = 100m,
            InstallmentCount = 2,
            OriginalFirstDueDate = new DateOnly(2026, 10, 15),
            CreatedAt = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            Version = 1
        });
        db.ManualTransactions.AddRange(
            new ManualTransactionEntity
            {
                Id = Guid.NewGuid(),
                Type = ManualTransactionType.Income,
                Description = "Entrada prevista",
                Amount = 200m,
                CategoryId = categoryId,
                CompetenceDate = new DateOnly(2026, 10, 1),
                ExpectedDate = new DateOnly(2026, 10, 10),
                Status = ManualTransactionStatus.Planned,
                Version = 1
            },
            new ManualTransactionEntity
            {
                Id = Guid.NewGuid(),
                Type = ManualTransactionType.Expense,
                Description = "Despesa realizada",
                Amount = 30m,
                CategoryId = categoryId,
                CompetenceDate = new DateOnly(2026, 9, 1),
                ExpectedDate = new DateOnly(2026, 9, 25),
                EffectiveDate = new DateOnly(2026, 10, 2),
                Status = ManualTransactionStatus.Realized,
                Version = 1
            },
            new ManualTransactionEntity
            {
                Id = Guid.NewGuid(),
                Type = ManualTransactionType.Expense,
                Description = "Parcela 1",
                Amount = 50m,
                CategoryId = categoryId,
                CompetenceDate = new DateOnly(2026, 10, 1),
                ExpectedDate = new DateOnly(2026, 10, 15),
                Status = ManualTransactionStatus.Planned,
                InstallmentPurchaseId = purchaseId,
                InstallmentNumber = 1,
                Version = 1
            },
            new ManualTransactionEntity
            {
                Id = Guid.NewGuid(),
                Type = ManualTransactionType.Expense,
                Description = "Parcela 2",
                Amount = 50m,
                CategoryId = categoryId,
                CompetenceDate = new DateOnly(2026, 11, 1),
                ExpectedDate = new DateOnly(2026, 11, 15),
                Status = ManualTransactionStatus.Cancelled,
                StatusBeforeCancellation = ManualTransactionStatus.Planned,
                InstallmentPurchaseId = purchaseId,
                InstallmentNumber = 2,
                Version = 1
            });
        await db.SaveChangesAsync();
        return (categoryId, purchaseId);
    }
}