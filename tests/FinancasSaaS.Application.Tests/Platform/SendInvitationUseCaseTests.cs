using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Platform.Invitations;
using Xunit;

namespace FinancasSaaS.Application.Tests.Platform;

public sealed class SendInvitationUseCaseTests
{
    [Fact]
    public async Task EnviaLinkSemSenha()
    {
        var email = new FakeEmailSender();
        var useCase = new SendInvitationUseCase(
            new IssueInvitationUseCase(new FakeStore(), new FixedTokenGenerator(), new FakeHasher(), new FixedClock()),
            email);

        await useCase.ExecuteAsync(new SendInvitationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "cliente@example.com", "https://app.local"));

        Assert.Contains("conclua seu cadastro", email.Body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("senha", email.Body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakeEmailSender : IEmailSender
    {
        public string Body { get; private set; } = string.Empty;
        public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken = default)
        {
            Body = body;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStore : IInvitationStore
    {
        public Task<IssueInvitationResult> CreateAsync(Guid customerId, Guid tenantId, Guid userId, string recipient, string tokenHash, string actorId, DateTimeOffset issuedAt, DateTimeOffset expiresAt, CancellationToken cancellationToken = default) => Task.FromResult(new IssueInvitationResult(Guid.NewGuid(), string.Empty, expiresAt));
    }

    private sealed class FixedTokenGenerator : ITokenGenerator
    {
        public string Generate() => "token";
    }

    private sealed class FakeHasher : ITokenHasher
    {
        public string Hash(string token) => $"hash:{token}";
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    }
}
