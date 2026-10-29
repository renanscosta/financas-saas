namespace FinancasSaaS.Application.Abstractions;

public interface ICurrentUserContext
{
    Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default);
    Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default);
}
