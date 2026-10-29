using System.Security.Claims;
using FinancasSaaS.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

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
    ITenantConnectionResolver tenantConnectionResolver,
    ISystemClock clock,
    ILogger<TenantAccessAuthorizationHandler> logger)
    : AuthorizationHandler<TenantAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantAccessRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            LogDenied(null);
            return;
        }

        try
        {
            var connection = await tenantConnectionResolver.ResolveAsync(userId);
            if (!string.IsNullOrWhiteSpace(connection))
            {
                context.Succeed(requirement);
                return;
            }

            LogDenied(userId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                FinanceSecurityLog.ResolutionFailed(
                    logger,
                    exception,
                    userId,
                    clock.UtcNow.ToUniversalTime(),
                    System.Diagnostics.Activity.Current?.TraceId.ToString());
            }

            throw;
        }
    }

    private void LogDenied(Guid? actorId)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            FinanceSecurityLog.AccessDenied(
                logger,
                actorId,
                clock.UtcNow.ToUniversalTime(),
                System.Diagnostics.Activity.Current?.TraceId.ToString());
        }
    }
}

internal static partial class FinanceSecurityLog
{
    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Tenant access denied. ActorId={ActorId} OccurredAtUtc={OccurredAtUtc} CorrelationId={CorrelationId}")]
    public static partial void AccessDenied(
        ILogger logger,
        Guid? actorId,
        DateTimeOffset occurredAtUtc,
        string? correlationId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Tenant access resolution failed. ActorId={ActorId} OccurredAtUtc={OccurredAtUtc} CorrelationId={CorrelationId}")]
    public static partial void ResolutionFailed(
        ILogger logger,
        Exception exception,
        Guid actorId,
        DateTimeOffset occurredAtUtc,
        string? correlationId);
}
