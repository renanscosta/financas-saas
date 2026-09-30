using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Platform.Infrastructure.Persistence;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<TenantEntity> Tenants => Set<TenantEntity>();
    public DbSet<ProvisioningEntity> Provisionings => Set<ProvisioningEntity>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<InvitationEntity> Invitations => Set<InvitationEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CustomerEntity>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Email).IsUnique();
            entity.Property(item => item.Email).HasMaxLength(320).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
        });

        builder.Entity<TenantEntity>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.CustomerId).IsUnique();
            entity.Property(item => item.DatabaseName).HasMaxLength(128).IsRequired();
            entity.Property(item => item.ConnectionSecretReference).HasMaxLength(256).IsRequired();
            entity.Property(item => item.ProvisioningStatus).HasConversion<string>().HasMaxLength(32);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasOne<CustomerEntity>()
                .WithMany()
                .HasForeignKey(item => item.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProvisioningEntity>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.TenantId).IsUnique();
            entity.Property(item => item.LastFailure).HasMaxLength(2000);
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
            entity.HasOne<TenantEntity>()
                .WithMany()
                .HasForeignKey(item => item.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditEntry>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EventName).HasMaxLength(128).IsRequired();
            entity.Property(item => item.ActorId).HasMaxLength(256);
            entity.Property(item => item.Reason).HasMaxLength(2000);
        });

        builder.Entity<InvitationEntity>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.TokenHash).IsUnique();
            entity.Property(item => item.TokenHash).HasMaxLength(128).IsRequired();
            entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
            entity.Property(item => item.InvalidationReason).HasMaxLength(128);
            entity.HasOne<CustomerEntity>()
                .WithMany()
                .HasForeignKey(item => item.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TenantEntity>()
                .WithMany()
                .HasForeignKey(item => item.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ApplicationUser>()
            .HasOne<TenantEntity>()
            .WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AuditEntry>()
            .HasOne<TenantEntity>()
            .WithMany()
            .HasForeignKey(item => item.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
