using System.Security.Claims;
using FinancasSaaS.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace FinancasSaaS.Web.Security;

public sealed class ProvisioningRetryService(
    IAuthorizationService authorizationService,
    ITenantProvisioner provisioner)
{
    public async Task<bool> ExecuteAsync(ClaimsPrincipal user, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var authorization = await authorizationService.AuthorizeAsync(user, null, PlatformPolicies.MutateProvisioning);
        if (!authorization.Succeeded)
        {
            return false;
        }

        await provisioner.ProvisionAsync(tenantId, cancellationToken);
        return true;
    }
}