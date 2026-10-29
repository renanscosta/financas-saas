using FinancasSaaS.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Finance.Infrastructure.Persistence;

public sealed class TenantFinanceContextFactory(
    ICurrentUserContext currentUser,
    ITenantConnectionResolver tenantConnectionResolver) : ITenantFinanceContextFactory
{
    public async Task<TenantDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        var userId = await currentUser.GetUserIdAsync(cancellationToken);
        if (userId is not Guid authenticatedUserId || authenticatedUserId == Guid.Empty)
        {
            throw new InvalidOperationException("A identidade autenticada não está disponível.");
        }

        var connectionString = await tenantConnectionResolver.ResolveAsync(authenticatedUserId, cancellationToken);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("O usuário não possui acesso a um tenant pronto.");
        }

        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new TenantDbContext(options);
    }
}
