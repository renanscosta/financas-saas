using FinancasSaaS.Application.Abstractions;

namespace FinancasSaaS.Application.Platform.Invitations;

public sealed record IssueInvitationCommand(Guid CustomerId, Guid TenantId, Guid UserId, string Recipient = "", string ActorId = "system");
public sealed record IssueInvitationResult(Guid InvitationId, string Token, DateTimeOffset ExpiresAt);

public interface IInvitationStore
{
    Task<IssueInvitationResult> CreateAsync(Guid customerId, Guid tenantId, Guid userId, string recipient, string tokenHash, string actorId, DateTimeOffset issuedAt, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
}

public sealed class IssueInvitationUseCase(
    IInvitationStore store,
    ITokenGenerator tokenGenerator,
    ITokenHasher tokenHasher,
    ISystemClock clock)
{
    public async Task<IssueInvitationResult> ExecuteAsync(IssueInvitationCommand command, CancellationToken cancellationToken = default)
    {
        if (command.CustomerId == Guid.Empty || command.TenantId == Guid.Empty || command.UserId == Guid.Empty)
        {
            throw new ArgumentException("Cliente, tenant e usuário são obrigatórios.", nameof(command));
        }

        var issuedAt = clock.UtcNow;
        var expiresAt = issuedAt.AddHours(24);
        var token = tokenGenerator.Generate();
        return await store.CreateAsync(command.CustomerId, command.TenantId, command.UserId, command.Recipient, tokenHasher.Hash(token), command.ActorId, issuedAt, expiresAt, cancellationToken) with { Token = token };
    }
}
