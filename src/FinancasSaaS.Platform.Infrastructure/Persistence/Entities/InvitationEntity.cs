using FinancasSaaS.Domain.Platform;

namespace FinancasSaaS.Platform.Infrastructure.Persistence.Entities;

public sealed class InvitationEntity
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public InvitationStatus Status { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public DateTimeOffset? InvalidatedAt { get; set; }
    public string? InvalidationReason { get; set; }
}
