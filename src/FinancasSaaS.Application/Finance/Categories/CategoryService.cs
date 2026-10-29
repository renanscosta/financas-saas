using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Application.Finance;
using Microsoft.Extensions.Logging;

namespace FinancasSaaS.Application.Finance.Categories;

public sealed class CategoryService(
    ICategoryStore store,
    ICurrentUserContext currentUser,
    ISystemClock clock,
    ILogger<CategoryService> logger)
{
    public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
        store.ListAsync(cancellationToken);

    public Task<Category?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        store.GetAsync(id, cancellationToken);

    public async Task<Category> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var actorId = await RequireActorAsync(cancellationToken);
        var tenantId = await RequireTenantAsync(cancellationToken);
        try
        {
            var category = await store.CreateAsync(name, cancellationToken);
            Log("category.create", "success", actorId, tenantId);
            return category;
        }
        catch (CategoryNameConflictException)
        {
            Log("category.create", "conflict", actorId, tenantId);
            throw;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "category.create", actorId, tenantId);
            throw;
        }
    }

    public async Task<Category?> RenameAsync(Guid id, string name, CancellationToken cancellationToken = default)
    {
        var actorId = await RequireActorAsync(cancellationToken);
        var tenantId = await RequireTenantAsync(cancellationToken);
        try
        {
            var category = await store.RenameAsync(id, name, cancellationToken);
            Log("category.rename", category is null ? "not_found" : "success", actorId, tenantId);
            return category;
        }
        catch (CategoryNameConflictException)
        {
            Log("category.rename", "conflict", actorId, tenantId);
            throw;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "category.rename", actorId, tenantId);
            throw;
        }
    }

    public async Task<CategoryDeleteResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var actorId = await RequireActorAsync(cancellationToken);
        var tenantId = await RequireTenantAsync(cancellationToken);
        try
        {
            var result = await store.DeleteAsync(id, cancellationToken);
            Log("category.delete", result.ToString(), actorId, tenantId);
            return result;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "category.delete", actorId, tenantId);
            throw;
        }
    }

    private async Task<Guid> RequireActorAsync(CancellationToken cancellationToken)
    {
        var actorId = await currentUser.GetUserIdAsync(cancellationToken);
        return actorId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("A identidade autenticada não está disponível.");
    }

    private void Log(string operation, string result, Guid actorId, Guid? tenantId)
    {
        var correlationId = System.Diagnostics.Activity.Current?.TraceId.ToString();
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
                System.Diagnostics.Activity.Current?.TraceId.ToString());
        }
    }

    private async Task<Guid> RequireTenantAsync(CancellationToken cancellationToken)
    {
        var tenantId = await currentUser.GetTenantIdAsync(cancellationToken);
        return tenantId is Guid id && id != Guid.Empty
            ? id
            : throw new InvalidOperationException("O tenant autenticado não está disponível.");
    }
}
