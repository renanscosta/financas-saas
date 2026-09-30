using System.Security.Claims;
using FinancasSaaS.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;

namespace FinancasSaaS.Web.Security;

public static class PlatformRoles
{
    public const string ReadOnlyOperator = "platform.readonly";
    public const string Administrator = "platform.admin";
}

public static class PlatformPolicies
{
    public const string ReadProvisioning = "platform.provisioning.read";
    public const string MutateProvisioning = "platform.provisioning.mutate";
    public const string TenantAccess = "tenant.access";
}

public sealed class TenantAccessRequirement : IAuthorizationRequirement;

public sealed class TenantAccessAuthorizationHandler(
    ITenantConnectionResolver tenantConnectionResolver)
    : AuthorizationHandler<TenantAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantAccessRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return;
        }

        var connection = await tenantConnectionResolver.ResolveAsync(userId);
        if (!string.IsNullOrWhiteSpace(connection))
        {
            context.Succeed(requirement);
        }
    }
}
