using System.Text.Json;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance.ManualTransactions;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Finance.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Finance.Infrastructure.ManualTransactions;

public sealed class ManualTransactionStore(ITenantFinanceContextFactory contextFactory) : IManualTransactionStore
{
    public async Task<ManualTransactionRecord> CreateAsync(
        ManualTransaction transaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var category = await db.Categories.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == transaction.CategoryId && item.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("A categoria não existe ou não está disponível neste tenant.");
        var entity = ToEntity(transaction);
        entity.Version = 1;
        db.ManualTransactions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        entity.Category = category;
        return ToRecord(entity);
    }

    public async Task<ManualTransactionRecord?> GetAsync(Guid transactionId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ManualTransactions.AsNoTracking()
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == transactionId, cancellationToken);
        return entity is null ? null : ToRecord(entity);
    }

    public async Task<IReadOnlyList<ManualTransactionRecord>> ListAsync(
        ManualTransactionFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (filter.StartDate > filter.EndDate)
        {
            throw new ArgumentException("A data inicial deve ser anterior ou igual à data final.", nameof(filter));
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.ManualTransactions
            .AsNoTracking()
            .Include(item => item.Category)
            .Where(item =>
                item.Status == ManualTransactionStatus.Planned &&
                    item.ExpectedDate >= filter.StartDate && item.ExpectedDate <= filter.EndDate ||
                item.Status == ManualTransactionStatus.Realized &&
                    item.EffectiveDate >= filter.StartDate && item.EffectiveDate <= filter.EndDate ||
                item.Status == ManualTransactionStatus.Cancelled &&
                    (item.StatusBeforeCancellation == ManualTransactionStatus.Planned &&
                        item.ExpectedDate >= filter.StartDate && item.ExpectedDate <= filter.EndDate ||
                     item.StatusBeforeCancellation == ManualTransactionStatus.Realized &&
                        item.EffectiveDate >= filter.StartDate && item.EffectiveDate <= filter.EndDate));

        if (filter.Type is { } type)
        {
            query = query.Where(item => item.Type == type);
        }

        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(item => item.CategoryId == categoryId);
        }

        if (filter.Status is { } status)
        {
            query = query.Where(item => item.Status == status);
        }

        var entities = await query
            .OrderByDescending(item =>
                item.Status == ManualTransactionStatus.Realized ||
                item.Status == ManualTransactionStatus.Cancelled &&
                item.StatusBeforeCancellation == ManualTransactionStatus.Realized
                    ? item.EffectiveDate
                    : item.ExpectedDate)
            .ThenBy(item => item.Description)
            .ToListAsync(cancellationToken);
        return entities.Select(ToRecord).ToArray();
    }

    public async Task<ManualTransactionRecord?> RealizeAsync(
        Guid transactionId,
        DateOnly effectiveDate,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ManualTransactions
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == transactionId, cancellationToken);
        if (entity is null || entity.Version != expectedVersion)
        {
            return null;
        }

        var transaction = ToDomain(entity);
        transaction.Realize(effectiveDate);
        Apply(transaction, entity);
        entity.Version++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }

        return ToRecord(entity);
    }

    public async Task<ManualTransactionRecord?> CorrectAsync(
        Guid transactionId,
        ManualTransactionCorrection correction,
        long expectedVersion,
        DateTimeOffset occurredAt,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ManualTransactions
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == transactionId, cancellationToken);
        if (entity is null || entity.Version != expectedVersion)
        {
            return null;
        }

        if (correction.CategoryId is { } categoryId &&
            !await db.Categories.AnyAsync(item => item.Id == categoryId && item.IsActive, cancellationToken))
        {
            throw new InvalidOperationException("A categoria não existe ou não está disponível neste tenant.");
        }

        var transaction = ToDomain(entity);
        var audit = transaction.Correct(correction, occurredAt, actorId);
        Apply(transaction, entity);
        entity.Version++;
        db.ManualTransactionAuditEvents.Add(ToEntity(audit));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }

        entity.Category = await db.Categories.AsNoTracking()
            .SingleAsync(item => item.Id == entity.CategoryId, cancellationToken);
        return ToRecord(entity);
    }

    public async Task<ManualTransactionRecord?> CancelAsync(
        Guid transactionId,
        string reason,
        long expectedVersion,
        DateTimeOffset occurredAt,
        Guid actorId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.ManualTransactions
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.Id == transactionId, cancellationToken);
        if (entity is null || entity.Version != expectedVersion)
        {
            return null;
        }

        var transaction = ToDomain(entity);
        var audit = transaction.Cancel(occurredAt, actorId, reason);
        Apply(transaction, entity);
        entity.Version++;
        db.ManualTransactionAuditEvents.Add(ToEntity(audit));
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }

        return ToRecord(entity);
    }

    public async Task<IReadOnlyList<ManualTransactionAuditEvent>> GetHistoryAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var entities = await db.ManualTransactionAuditEvents
            .AsNoTracking()
            .Where(item => item.TransactionId == transactionId)
            .OrderBy(item => item.OccurredAt)
            .ToListAsync(cancellationToken);
        return entities.Select(ToDomain).ToArray();
    }

    private static ManualTransactionRecord ToRecord(ManualTransactionEntity entity)
    {
        if (entity.Category is null)
        {
            throw new InvalidOperationException("O lançamento não possui categoria carregada.");
        }

        return new ManualTransactionRecord(ToDomain(entity), entity.Version, entity.Category.Name);
    }

    private static ManualTransaction ToDomain(ManualTransactionEntity entity) =>
        ManualTransaction.Restore(
            entity.Id,
            entity.Type,
            entity.Description,
            entity.Amount,
            entity.CategoryId,
            entity.CompetenceDate,
            entity.ExpectedDate,
            entity.Status,
            entity.EffectiveDate,
            entity.StatusBeforeCancellation);

    private static ManualTransactionEntity ToEntity(ManualTransaction transaction) => new()
    {
        Id = transaction.Id,
        Type = transaction.Type,
        Description = transaction.Description,
        Amount = transaction.Amount,
        CategoryId = transaction.CategoryId,
        CompetenceDate = transaction.CompetenceDate,
        ExpectedDate = transaction.ExpectedDate,
        Status = transaction.Status,
        EffectiveDate = transaction.EffectiveDate,
        StatusBeforeCancellation = transaction.StatusBeforeCancellation
    };

    private static void Apply(ManualTransaction transaction, ManualTransactionEntity entity)
    {
        entity.Type = transaction.Type;
        entity.Description = transaction.Description;
        entity.Amount = transaction.Amount;
        entity.CategoryId = transaction.CategoryId;
        entity.CompetenceDate = transaction.CompetenceDate;
        entity.ExpectedDate = transaction.ExpectedDate;
        entity.Status = transaction.Status;
        entity.EffectiveDate = transaction.EffectiveDate;
        entity.StatusBeforeCancellation = transaction.StatusBeforeCancellation;
    }

    private static ManualTransactionAuditEventEntity ToEntity(ManualTransactionAuditEvent audit) => new()
    {
        Id = audit.Id,
        TransactionId = audit.TransactionId,
        EventType = audit.EventType,
        ChangesJson = JsonSerializer.Serialize(audit.Changes),
        OccurredAt = audit.OccurredAt,
        ActorId = audit.ActorId,
        Reason = audit.Reason
    };

    private static ManualTransactionAuditEvent ToDomain(ManualTransactionAuditEventEntity entity) =>
        new(
            entity.Id,
            entity.TransactionId,
            entity.EventType,
            JsonSerializer.Deserialize<List<ManualTransactionChange>>(entity.ChangesJson)
                ?? throw new InvalidOperationException("O histórico do lançamento está malformado."),
            entity.OccurredAt,
            entity.ActorId,
            entity.Reason);
}
