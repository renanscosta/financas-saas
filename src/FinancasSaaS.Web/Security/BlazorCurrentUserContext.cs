using System.Security.Claims;
using FinancasSaaS.Application.Abstractions;
using Microsoft.AspNetCore.Components.Authorization;

namespace FinancasSaaS.Web.Security;

public sealed class BlazorCurrentUserContext(
    AuthenticationStateProvider authenticationStateProvider,
    ITenantConnectionResolver tenantConnectionResolver)
    : ICurrentUserContext
{
    public async Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
        return Guid.TryParse(authenticationState.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;
    }

    public async Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default)
    {
        var userId = await GetUserIdAsync(cancellationToken);
        return userId is Guid id
            ? await tenantConnectionResolver.ResolveTenantIdAsync(id, cancellationToken)
            : null;
    }
}
