using System.Text.Json;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance.InstallmentPurchases;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Finance.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Finance.Infrastructure.InstallmentPurchases;

public sealed class InstallmentPurchaseStore(ITenantFinanceContextFactory contextFactory) : IInstallmentPurchaseStore
{
    public async Task<InstallmentPurchaseRecord> CreateAsync(
        InstallmentPurchase purchase,
        InstallmentPurchaseAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purchase);
        ArgumentNullException.ThrowIfNull(auditEvent);
        if (auditEvent.PurchaseId != purchase.Id || auditEvent.EventType != "Creation")
        {
            throw new ArgumentException("O evento inicial deve pertencer à compra criada.", nameof(auditEvent));
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == purchase.CategoryId && item.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("A categoria não existe ou não está disponível neste tenant.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var entity = ToEntity(purchase);
        db.Attach(category);
        entity.Category = category;
        entity.Installments = purchase.Installments.Select(item =>
        {
            var installment = ToEntity(item, entity);
            installment.Category = category;
            return installment;
        }).ToList();
        entity.AuditEvents.Add(ToEntity(auditEvent));
        db.InstallmentPurchases.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToRecord(entity);
    }

    public async Task<InstallmentPurchaseRecord?> GetAsync(
        Guid purchaseId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.InstallmentPurchases
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Installments)
                .ThenInclude(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == purchaseId, cancellationToken);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<InstallmentPurchaseRecord>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await db.InstallmentPurchases
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Installments)
                .ThenInclude(item => item.Category)
            .OrderByDescending(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        return entities.Select(ToRecord).ToArray();
    }

    public async Task<InstallmentPurchaseRecord?> RealizeAsync(
        Guid purchaseId,
        int installmentNumber,
        DateOnly effectiveDate,
        long expectedVersion,
        DateTimeOffset occurredAt,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await LoadTrackedAsync(db, purchaseId, cancellationToken);
        if (entity is null || entity.Version != expectedVersion)
        {
            return null;
        }

        var purchase = ToDomain(entity);
        var audit = purchase.Realize(installmentNumber, effectiveDate, occurredAt, actorId);
        Apply(purchase, entity);
        db.InstallmentPurchaseAuditEvents.Add(ToEntity(audit));
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }

        return ToRecord(entity);
    }

    public async Task<InstallmentPurchaseRecord?> EditAsync(
        Guid purchaseId,
        string description,
        Guid categoryId,
        decimal totalAmount,
        DateOnly? firstEligibleDueDate,
        DateOnly operationDate,
        string reason,
        long expectedVersion,
        DateTimeOffset occurredAt,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await LoadTrackedAsync(db, purchaseId, cancellationToken);
        if (entity is null || entity.Version != expectedVersion)
        {
            return null;
        }

        var category = await db.Categories
            .SingleOrDefaultAsync(item => item.Id == categoryId && item.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("A categoria não existe ou não está disponível neste tenant.");

        var purchase = ToDomain(entity);
        var audit = purchase.Edit(
            description,
            categoryId,
            totalAmount,
            firstEligibleDueDate,
            operationDate,
            reason,
            occurredAt,
            actorId);
        Apply(purchase, entity);
        db.InstallmentPurchaseAuditEvents.Add(ToEntity(audit));
        entity.Category = category;
        foreach (var installmentEntity in entity.Installments.Where(item => item.CategoryId == categoryId))
        {
            installmentEntity.Category = category;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }

        return ToRecord(entity);
    }

    public async Task<InstallmentPurchaseRecord?> CancelAsync(
        Guid purchaseId,
        DateOnly operationDate,
        string reason,
        long expectedVersion,
        DateTimeOffset occurredAt,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await LoadTrackedAsync(db, purchaseId, cancellationToken);
        if (entity is null || entity.Version != expectedVersion)
        {
            return null;
        }

        var purchase = ToDomain(entity);
        var audit = purchase.Cancel(operationDate, reason, occurredAt, actorId);
        Apply(purchase, entity);
        db.InstallmentPurchaseAuditEvents.Add(ToEntity(audit));

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }

        return ToRecord(entity);
    }

    public async Task<IReadOnlyList<InstallmentPurchaseAuditEvent>> GetHistoryAsync(
        Guid purchaseId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await db.InstallmentPurchaseAuditEvents
            .AsNoTracking()
            .Where(item => item.PurchaseId == purchaseId)
            .OrderBy(item => item.OccurredAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToArray();
    }

    private static Task<InstallmentPurchaseEntity?> LoadTrackedAsync(
        TenantDbContext db,
        Guid purchaseId,
        CancellationToken cancellationToken) =>
        db.InstallmentPurchases
            .Include(item => item.Category)
            .Include(item => item.Installments)
                .ThenInclude(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == purchaseId, cancellationToken);

    private static InstallmentPurchaseEntity ToEntity(InstallmentPurchase purchase) => new()
    {
        Id = purchase.Id,
        Description = purchase.Description,
        CategoryId = purchase.CategoryId,
        TotalAmount = purchase.TotalAmount,
        InstallmentCount = purchase.InstallmentCount,
        OriginalFirstDueDate = purchase.OriginalFirstDueDate,
        CreatedAt = purchase.CreatedAt,
        Version = purchase.Version
    };

    private static ManualTransactionEntity ToEntity(
        InstallmentPurchaseInstallment item,
        InstallmentPurchaseEntity purchase) => new()
    {
        Id = item.TransactionId,
        Type = ManualTransactionType.Expense,
        Description = item.Description,
        Amount = item.Amount,
        CategoryId = item.CategoryId,
        CompetenceDate = item.CompetenceDate,
        ExpectedDate = item.DueDate,
        Status = item.Status,
        EffectiveDate = item.EffectiveDate,
        StatusBeforeCancellation = item.StatusBeforeCancellation,
        Version = item.Version,
        InstallmentPurchaseId = purchase.Id,
        InstallmentNumber = item.Number,
        InstallmentPurchase = purchase
    };

    private static InstallmentPurchaseAuditEventEntity ToEntity(InstallmentPurchaseAuditEvent audit) => new()
    {
        Id = audit.Id,
        PurchaseId = audit.PurchaseId,
        EventType = audit.EventType,
        ChangesJson = JsonSerializer.Serialize(audit.Changes),
        OccurredAt = audit.OccurredAt,
        ActorId = audit.ActorId,
        Reason = audit.Reason
    };

    private static void Apply(InstallmentPurchase purchase, InstallmentPurchaseEntity entity)
    {
        entity.Description = purchase.Description;
        entity.CategoryId = purchase.CategoryId;
        entity.TotalAmount = purchase.TotalAmount;
        entity.Version = purchase.Version;
        var installments = purchase.Installments.ToDictionary(item => item.Number);
        foreach (var installmentEntity in entity.Installments)
        {
            Apply(installments[installmentEntity.InstallmentNumber!.Value], installmentEntity);
        }
    }

    private static void Apply(InstallmentPurchaseInstallment item, ManualTransactionEntity entity)
    {
        entity.Description = item.Description;
        entity.Amount = item.Amount;
        entity.CategoryId = item.CategoryId;
        entity.CompetenceDate = item.CompetenceDate;
        entity.ExpectedDate = item.DueDate;
        entity.Status = item.Status;
        entity.EffectiveDate = item.EffectiveDate;
        entity.StatusBeforeCancellation = item.StatusBeforeCancellation;
        entity.Version = item.Version;
    }

    private static InstallmentPurchaseRecord ToRecord(InstallmentPurchaseEntity entity)
    {
        if (entity.Category is null || entity.Installments.Any(item => item.Category is null))
        {
            throw new InvalidOperationException("A compra parcelada não possui categorias carregadas.");
        }

        var purchase = ToDomain(entity);
        var installmentsByNumber = purchase.Installments.ToDictionary(item => item.Number);
        var installments = entity.Installments
            .OrderBy(item => item.InstallmentNumber)
            .Select(item => new InstallmentPurchaseInstallmentRecord(
                installmentsByNumber[item.InstallmentNumber
                    ?? throw new InvalidOperationException("A despesa associada não possui número de parcela.")],
                item.Category!.Name))
            .ToArray();
        return new InstallmentPurchaseRecord(purchase, entity.Category.Name, installments);
    }

    private static InstallmentPurchase ToDomain(InstallmentPurchaseEntity entity)
    {
        var installments = entity.Installments.Select(item =>
        {
            if (item.Type != ManualTransactionType.Expense)
            {
                throw new InvalidOperationException("Uma parcela persistida precisa ser uma despesa.");
            }

            return new InstallmentPurchaseInstallment(
                item.Id,
                item.InstallmentNumber
                    ?? throw new InvalidOperationException("A despesa associada não possui número de parcela."),
                item.Description,
                item.Amount,
                item.CategoryId,
                item.ExpectedDate,
                item.CompetenceDate,
                item.Status,
                item.EffectiveDate,
                item.StatusBeforeCancellation,
                item.Version);
        });

        return InstallmentPurchase.Restore(
            entity.Id,
            entity.Description,
            entity.CategoryId,
            entity.TotalAmount,
            entity.InstallmentCount,
            entity.OriginalFirstDueDate,
            entity.CreatedAt,
            entity.Version,
            installments);
    }

    private static InstallmentPurchaseAuditEvent ToDomain(InstallmentPurchaseAuditEventEntity entity) =>
        new(
            entity.Id,
            entity.PurchaseId,
            entity.EventType,
            JsonSerializer.Deserialize<List<InstallmentPurchaseAuditChange>>(entity.ChangesJson)
                ?? throw new InvalidOperationException("O histórico do parcelamento está malformado."),
            entity.OccurredAt,
            entity.ActorId,
            entity.Reason);
}
