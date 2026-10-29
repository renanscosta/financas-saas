using Microsoft.Extensions.Logging;

namespace FinancasSaaS.Application.Finance;

internal static partial class FinanceLog
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Finance operation completed. Operation={Operation} Result={Result} ActorId={ActorId} TenantId={TenantId} OccurredAtUtc={OccurredAtUtc} CorrelationId={CorrelationId}")]
    public static partial void OperationCompleted(
        ILogger logger,
        string operation,
        string result,
        Guid actorId,
        Guid? tenantId,
        DateTimeOffset occurredAtUtc,
        string? correlationId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Finance persistence operation failed. Operation={Operation} ActorId={ActorId} TenantId={TenantId} OccurredAtUtc={OccurredAtUtc} CorrelationId={CorrelationId}")]
    public static partial void PersistenceFailed(
        ILogger logger,
        Exception exception,
        string operation,
        Guid actorId,
        Guid? tenantId,
        DateTimeOffset occurredAtUtc,
        string? correlationId);
}
