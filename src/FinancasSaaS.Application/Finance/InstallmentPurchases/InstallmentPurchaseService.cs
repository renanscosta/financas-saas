using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Domain.Finance;
using Microsoft.Extensions.Logging;

namespace FinancasSaaS.Application.Finance.InstallmentPurchases;

public sealed class InstallmentPurchaseService(
    IInstallmentPurchaseStore store,
    ICurrentUserContext currentUser,
    ISystemClock clock,
    ILogger<InstallmentPurchaseService> logger)
{
    public async Task<InstallmentPurchaseRecord> CreateAsync(
        string description,
        Guid categoryId,
        decimal totalAmount,
        int installmentCount,
        DateOnly firstDueDate,
        CancellationToken cancellationToken = default)
    {
        var (actorId, tenantId) = await GetCurrentContextAsync(cancellationToken);
        var occurredAt = clock.UtcNow.ToUniversalTime();
        var purchase = InstallmentPurchase.Create(
            Guid.NewGuid(),
            description,
            categoryId,
            totalAmount,
            installmentCount,
            firstDueDate,
            occurredAt);
        var audit = purchase.CreateInitialAuditEvent(occurredAt, actorId);
        try
        {
            var result = await store.CreateAsync(purchase, audit, cancellationToken);
            LogMutation("installment_purchase.create", "success", actorId, tenantId);
            return result;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "installment_purchase.create", actorId, tenantId);
            throw;
        }
    }

    public async Task<InstallmentPurchaseRecord?> GetAsync(
        Guid purchaseId,
        CancellationToken cancellationToken = default)
    {
        var (actorId, tenantId) = await GetCurrentContextAsync(cancellationToken);
        try
        {
            var result = await store.GetAsync(purchaseId, cancellationToken);
            LogMutation("installment_purchase.get", result is null ? "not_found" : "success", actorId, tenantId);
            return result;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "installment_purchase.get", actorId, tenantId);
            throw;
        }
    }

    public async Task<IReadOnlyList<InstallmentPurchaseRecord>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var (actorId, tenantId) = await GetCurrentContextAsync(cancellationToken);
        try
        {
            var result = await store.ListAsync(cancellationToken);
            LogMutation("installment_purchase.list", "success", actorId, tenantId);
            return result;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "installment_purchase.list", actorId, tenantId);
            throw;
        }
    }

    public async Task<InstallmentPurchaseRecord?> RealizeAsync(
        Guid purchaseId,
        int installmentNumber,
        DateOnly effectiveDate,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var (actorId, tenantId) = await GetCurrentContextAsync(cancellationToken);
        try
        {
            var result = await store.RealizeAsync(
                purchaseId,
                installmentNumber,
                effectiveDate,
                expectedVersion,
                clock.UtcNow.ToUniversalTime(),
                actorId,
                cancellationToken);
            LogMutation(
                "installment_purchase.realize",
                result is null ? "conflict_or_not_found" : "success",
                actorId,
                tenantId);
            return result;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "installment_purchase.realize", actorId, tenantId);
            throw;
        }
    }

    public async Task<InstallmentPurchaseRecord?> EditAsync(
        Guid purchaseId,
        string description,
        Guid categoryId,
        decimal totalAmount,
        DateOnly? firstEligibleDueDate,
        string reason,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var (actorId, tenantId) = await GetCurrentContextAsync(cancellationToken);
        var occurredAt = clock.UtcNow.ToUniversalTime();
        try
        {
            var result = await store.EditAsync(
                purchaseId,
                description,
                categoryId,
                totalAmount,
                firstEligibleDueDate,
                DateOnly.FromDateTime(occurredAt.UtcDateTime),
                reason,
                expectedVersion,
                occurredAt,
                actorId,
                cancellationToken);
            LogMutation("installment_purchase.edit", result is null ? "conflict_or_not_found" : "success", actorId, tenantId);
            return result;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "installment_purchase.edit", actorId, tenantId);
            throw;
        }
    }

    public async Task<InstallmentPurchaseRecord?> CancelAsync(
        Guid purchaseId,
        string reason,
        long expectedVersion,
        CancellationToken cancellationToken = default)
    {
        var (actorId, tenantId) = await GetCurrentContextAsync(cancellationToken);
        var occurredAt = clock.UtcNow.ToUniversalTime();
        try
        {
            var result = await store.CancelAsync(
                purchaseId,
                DateOnly.FromDateTime(occurredAt.UtcDateTime),
                reason,
                expectedVersion,
                occurredAt,
                actorId,
                cancellationToken);
            LogMutation("installment_purchase.cancel", result is null ? "conflict_or_not_found" : "success", actorId, tenantId);
            return result;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "installment_purchase.cancel", actorId, tenantId);
            throw;
        }
    }

    public async Task<IReadOnlyList<InstallmentPurchaseAuditEvent>> GetHistoryAsync(
        Guid purchaseId,
        CancellationToken cancellationToken = default)
    {
        var (actorId, tenantId) = await GetCurrentContextAsync(cancellationToken);
        try
        {
            var result = await store.GetHistoryAsync(purchaseId, cancellationToken);
            LogMutation("installment_purchase.history", "success", actorId, tenantId);
            return result;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "installment_purchase.history", actorId, tenantId);
            throw;
        }
    }

    private async Task<(Guid ActorId, Guid TenantId)> GetCurrentContextAsync(CancellationToken cancellationToken)
    {
        var actorId = await currentUser.GetUserIdAsync(cancellationToken);
        if (actorId is not Guid authenticatedActorId || authenticatedActorId == Guid.Empty)
        {
            throw new InvalidOperationException("A identidade autenticada não está disponível.");
        }

        var tenantId = await currentUser.GetTenantIdAsync(cancellationToken);
        if (tenantId is not Guid authenticatedTenantId || authenticatedTenantId == Guid.Empty)
        {
            throw new InvalidOperationException("O tenant autenticado não está disponível.");
        }

        return (authenticatedActorId, authenticatedTenantId);
    }

    private void LogMutation(string operation, string result, Guid actorId, Guid tenantId)
    {
        var occurredAtUtc = clock.UtcNow.ToUniversalTime();
        var correlationId = System.Diagnostics.Activity.Current?.TraceId.ToString();
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

    private void LogFailure(Exception exception, string operation, Guid actorId, Guid tenantId)
    {
        var occurredAtUtc = clock.UtcNow.ToUniversalTime();
        var correlationId = System.Diagnostics.Activity.Current?.TraceId.ToString();
        if (logger.IsEnabled(LogLevel.Error))
        {
            FinanceLog.PersistenceFailed(
                logger,
                exception,
                operation,
                actorId,
                tenantId,
                occurredAtUtc,
                correlationId);
        }
    }
}
