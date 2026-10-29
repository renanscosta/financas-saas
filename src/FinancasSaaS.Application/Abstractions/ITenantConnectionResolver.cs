namespace FinancasSaaS.Application.Abstractions;

public interface ITenantConnectionResolver
{
    Task<string?> ResolveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Guid?> ResolveTenantIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
