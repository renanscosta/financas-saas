using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Domain.Finance;
using Microsoft.Extensions.Logging;
using FinancasSaaS.Application.Finance;

namespace FinancasSaaS.Application.Finance.ManualTransactions;

public sealed class ManualTransactionService(
    IManualTransactionStore store,
    ICurrentUserContext currentUser,
    ISystemClock clock,
    ILogger<ManualTransactionService> logger)
{
    public Task<ManualTransactionRecord?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        store.GetAsync(id, cancellationToken);

    public Task<IReadOnlyList<ManualTransactionRecord>> ListAsync(
        ManualTransactionFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        if (filter.StartDate > filter.EndDate)
        {
            throw new ArgumentException("A data inicial deve ser anterior ou igual à data final.", nameof(filter));
        }

        return store.ListAsync(filter, cancellationToken);
    }

    public async Task<ManualTransactionRecord> CreateAsync(
        ManualTransactionType type,
        string description,
        decimal amount,
        Guid categoryId,
        DateOnly competenceDate,
        DateOnly expectedDate,
        CancellationToken cancellationToken = default)
    {
        var actorId = await RequireActorAsync(cancellationToken);
        var tenantId = await RequireTenantAsync(cancellationToken);
        var transaction = ManualTransaction.Create(
            Guid.NewGuid(),
            type,
            description,
            amount,
            categoryId,
            competenceDate,
            expectedDate);
        try
        {
            var created = await store.CreateAsync(transaction, cancellationToken);
            LogMutation("transaction.create", "success", actorId, tenantId);
            return created;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "transaction.create", actorId, tenantId);
            throw;
        }
    }

    public async Task<ManualTransactionRecord?> RealizeAsync(
        Guid id,
        DateOnly effectiveDate,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var actorId = await RequireActorAsync(cancellationToken);
        var tenantId = await RequireTenantAsync(cancellationToken);
        try
        {
            var result = await store.RealizeAsync(id, effectiveDate, expectedVersion, cancellationToken);
            LogMutation("transaction.realize", result is null ? "conflict_or_not_found" : "success", actorId, tenantId);
            return result;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "transaction.realize", actorId, tenantId);
            throw;
        }
    }

    public async Task<ManualTransactionRecord?> CorrectAsync(
        Guid id,
        ManualTransactionCorrection correction,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var actorId = await RequireActorAsync(cancellationToken);
        var tenantId = await RequireTenantAsync(cancellationToken);
        try
        {
            var result = await store.CorrectAsync(
                id,
                correction,
                expectedVersion,
                clock.UtcNow.ToUniversalTime(),
                actorId,
                cancellationToken);
            LogMutation("transaction.correct", result is null ? "conflict_or_not_found" : "success", actorId, tenantId);
            return result;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "transaction.correct", actorId, tenantId);
            throw;
        }
    }

    public async Task<ManualTransactionRecord?> CancelAsync(
        Guid id,
        string reason,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var actorId = await RequireActorAsync(cancellationToken);
        var tenantId = await RequireTenantAsync(cancellationToken);
        try
        {
            var result = await store.CancelAsync(
                id,
                reason,
                expectedVersion,
                clock.UtcNow.ToUniversalTime(),
                actorId,
                cancellationToken);
            LogMutation("transaction.cancel", result is null ? "conflict_or_not_found" : "success", actorId, tenantId);
            return result;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "transaction.cancel", actorId, tenantId);
            throw;
        }
    }

    public Task<IReadOnlyList<ManualTransactionAuditEvent>> GetHistoryAsync(
        Guid transactionId,
        CancellationToken cancellationToken = default) =>
        store.GetHistoryAsync(transactionId, cancellationToken);

    private async Task<Guid> RequireActorAsync(CancellationToken cancellationToken)
    {
        var actorId = await currentUser.GetUserIdAsync(cancellationToken);
        return actorId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("A identidade autenticada não está disponível.");
    }

    private async Task<Guid> RequireTenantAsync(CancellationToken cancellationToken)
    {
        var tenantId = await currentUser.GetTenantIdAsync(cancellationToken);
        return tenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("O tenant autenticado não está disponível.");
    }

    private void LogMutation(string operation, string result, Guid actorId, Guid? tenantId)
    {
        var correlationId = GetCorrelationId();
        var occurredAtUtc = clock.UtcNow.ToUniversalTime();
        if (logger.IsEnabled(LogLevel.Information))
        {
            FinanceLog.OperationCompleted(
                logger,
                operation,
                result,
                actorId,
                tenantId,
                occurredAtUtc,
                correlationId);
        }
    }

    private void LogFailure(Exception exception, string operation, Guid actorId, Guid? tenantId)
    {
        var occurredAtUtc = clock.UtcNow.ToUniversalTime();
        if (logger.IsEnabled(LogLevel.Error))
        {
            FinanceLog.PersistenceFailed(
                logger,
                exception,
                operation,
                actorId,
                tenantId,
                occurredAtUtc,
                GetCorrelationId());
        }
    }

    private static string? GetCorrelationId() => System.Diagnostics.Activity.Current?.TraceId.ToString();
}
