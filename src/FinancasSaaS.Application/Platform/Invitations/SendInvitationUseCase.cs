using FinancasSaaS.Application.Abstractions;

namespace FinancasSaaS.Application.Platform.Invitations;

public sealed record SendInvitationCommand(Guid CustomerId, Guid TenantId, Guid UserId, string Recipient, string CompletionBaseUrl, string ActorId = "system");

public sealed class SendInvitationUseCase(
    IssueInvitationUseCase issueInvitation,
    IEmailSender emailSender)
{
    public async Task<IssueInvitationResult> ExecuteAsync(SendInvitationCommand command, CancellationToken cancellationToken = default)
    {
        var invitation = await issueInvitation.ExecuteAsync(
            new IssueInvitationCommand(command.CustomerId, command.TenantId, command.UserId, command.Recipient, command.ActorId), cancellationToken);
        var link = $"{command.CompletionBaseUrl.TrimEnd('/')}/concluir-cadastro?token={Uri.EscapeDataString(invitation.Token)}";
        var body = $"Conclua seu cadastro pelo link: {link}\nEste convite expira em 24 horas.";
        await emailSender.SendAsync(command.Recipient, "Conclua seu cadastro", body, cancellationToken);
        return invitation;
    }
}
