namespace FinancasSaaS.Application.Abstractions;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}

public interface ITokenHasher
{
    string Hash(string token);
}

public interface ITenantProvisioner
{
    Task ProvisionAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed record TenantProvisioningTarget(string DatabaseName, string ConnectionString);

public interface ITenantProvisioningCatalog
{
    Task<TenantProvisioningTarget?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ITenantProvisioningStateStore
{
    Task<bool> BeginAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task MarkReadyAsync(Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid tenantId, DateTimeOffset now, string failure, CancellationToken cancellationToken = default);
}

public interface IAuditWriter
{
    Task WriteAsync(string eventName, Guid? tenantId, string? actorId, string? reason, CancellationToken cancellationToken = default);
}

public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default);
}
