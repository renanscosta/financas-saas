using FinancasSaaS.Finance.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Finance.Infrastructure.Persistence;

public sealed class TenantDbContext(DbContextOptions<TenantDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<ManualTransactionEntity> ManualTransactions => Set<ManualTransactionEntity>();
    public DbSet<ManualTransactionAuditEventEntity> ManualTransactionAuditEvents => Set<ManualTransactionAuditEventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(120).IsRequired();
            entity.Property(item => item.NormalizedName)
                .HasComputedColumnSql("upper(btrim(\"Name\"))", stored: true);
            entity.HasIndex(item => item.NormalizedName).IsUnique();
        });

        modelBuilder.Entity<ManualTransactionEntity>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Type).HasConversion<string>().HasMaxLength(16);
            entity.Property(item => item.Description).IsRequired();
            entity.Property(item => item.Amount).HasPrecision(18, 2);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(16);
            entity.Property(item => item.StatusBeforeCancellation).HasConversion<string>().HasMaxLength(16);
            entity.Property(item => item.Version).IsConcurrencyToken();
            entity.HasOne(item => item.Category)
                .WithMany(item => item.Transactions)
                .HasForeignKey(item => item.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => new { item.Status, item.ExpectedDate });
            entity.HasIndex(item => new { item.Status, item.EffectiveDate });
        });

        modelBuilder.Entity<ManualTransactionAuditEventEntity>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EventType).HasMaxLength(32).IsRequired();
            entity.Property(item => item.ChangesJson).IsRequired();
            entity.Property(item => item.Reason).IsRequired();
            entity.HasOne(item => item.Transaction)
                .WithMany(item => item.AuditEvents)
                .HasForeignKey(item => item.TransactionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(item => new { item.TransactionId, item.OccurredAt });
        });
    }
}
