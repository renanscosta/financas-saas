using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Platform.CompleteRegistration;
using FinancasSaaS.Application.Platform.Invitations;
using Xunit;

namespace FinancasSaaS.Application.Tests.Platform;

public sealed class InvitationContractTests
{
    [Fact]
    public async Task EmissaoEConclusaoEncaminhamTokenDeFormaSeguraEAtivamAcesso()
    {
        var invitationStore = new RecordingInvitationStore();
        var issueInvitation = new IssueInvitationUseCase(
            invitationStore,
            new FixedTokenGenerator(),
            new PrefixTokenHasher(),
            new FixedClock());
        var completionStore = new RecordingCompletionStore();
        var completion = new CompleteRegistrationUseCase(completionStore, new PrefixTokenHasher(), new FixedClock());

        var issued = await issueInvitation.ExecuteAsync(new IssueInvitationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()));
        var completed = await completion.ExecuteAsync(new CompleteRegistrationCommand(issued.Token, "StrongPassword123!"));

        Assert.Equal("plain-token", issued.Token);
        Assert.Equal("hashed:plain-token", invitationStore.TokenHash);
        Assert.Equal(issued.ExpiresAt, invitationStore.ExpiresAt);
        Assert.Equal("hashed:plain-token", completionStore.TokenHash);
        Assert.Equal("StrongPassword123!", completionStore.Password);
        Assert.NotNull(completed);
    }

    [Fact]
    public async Task EnvioUsaLinkSemSenhaEInvalidaConvitesAnteriores()
    {
        var invitationStore = new RecordingInvitationStore();
        var emailSender = new RecordingEmailSender();
        var sendInvitation = new SendInvitationUseCase(
            new IssueInvitationUseCase(
                invitationStore,
                new FixedTokenGenerator(),
                new PrefixTokenHasher(),
                new FixedClock()),
            emailSender);

        await sendInvitation.ExecuteAsync(new SendInvitationCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "cliente@example.com",
            "https://app.example.com"));

        Assert.True(invitationStore.Invalidated);
        Assert.Contains("concluir-cadastro?token=plain-token", emailSender.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("senha", emailSender.Body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class RecordingInvitationStore : IInvitationStore
    {
        public bool Invalidated { get; private set; }
        public string? TokenHash { get; private set; }
        public DateTimeOffset ExpiresAt { get; private set; }

        public Task<IssueInvitationResult> CreateAsync(
            Guid customerId,
            Guid tenantId,
            Guid userId,
            string recipient,
            string tokenHash,
            string actorId,
            DateTimeOffset issuedAt,
            DateTimeOffset expiresAt,
            CancellationToken cancellationToken = default)
        {
            Invalidated = true;
            TokenHash = tokenHash;
            ExpiresAt = expiresAt;
            return Task.FromResult(new IssueInvitationResult(Guid.NewGuid(), string.Empty, expiresAt));
        }
    }

    private sealed class RecordingCompletionStore : IRegistrationCompletionStore
    {
        public string? TokenHash { get; private set; }
        public string? Password { get; private set; }

        public Task<CompleteRegistrationResult?> CompleteAsync(
            string tokenHash,
            string password,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            TokenHash = tokenHash;
            Password = password;
            return Task.FromResult<CompleteRegistrationResult?>(new CompleteRegistrationResult(Guid.NewGuid(), Guid.NewGuid()));
        }
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public string Body { get; private set; } = string.Empty;

        public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
        {
            Body = body;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class FixedTokenGenerator : ITokenGenerator
    {
        public string Generate() => "plain-token";
    }

    private sealed class PrefixTokenHasher : ITokenHasher
    {
        public string Hash(string token) => $"hashed:{token}";
    }
}