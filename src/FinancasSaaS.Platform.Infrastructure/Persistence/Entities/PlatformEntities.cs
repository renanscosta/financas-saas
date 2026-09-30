using FinancasSaaS.Domain.Platform;

namespace FinancasSaaS.Platform.Infrastructure.Persistence.Entities;

public sealed class CustomerEntity
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public CustomerStatus Status { get; set; }
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? DisabledAt { get; set; }
}

public sealed class TenantEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public required string DatabaseName { get; set; }
    public required string ConnectionSecretReference { get; set; }
    public ProvisioningStatus ProvisioningStatus { get; set; }
    public CustomerStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class ProvisioningEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public ProvisioningStatus Status { get; set; }
    public int Attempt { get; set; }
    public string? LastFailure { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset LastAttemptAt { get; set; }
}

public sealed class AuditEntry
{
    public Guid Id { get; set; }
    public string EventName { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public string? ActorId { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
