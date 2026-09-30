using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Platform.Invitations;
using Xunit;

namespace FinancasSaaS.Application.Tests.Platform;

public sealed class InvitationLifecycleTests
{
    [Fact]
    public async Task EmissaoDefineValidadeDe24HorasEInvalidaOAnterior()
    {
        var clock = new FixedClock(new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero));
        var store = new FakeInvitationStore();
        var useCase = new IssueInvitationUseCase(store, new FixedTokenGenerator("token-plain"), new Sha256TokenHasher(), clock);

        var result = await useCase.ExecuteAsync(new IssueInvitationCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()));

        Assert.Equal(clock.UtcNow.AddHours(24), result.ExpiresAt);
        Assert.Equal("token-plain", result.Token);
        Assert.Equal(1, store.Invalidations);
        Assert.NotEqual("token-plain", store.TokenHash);
    }

    private sealed class FixedClock(DateTimeOffset now) : ISystemClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class FixedTokenGenerator(string token) : ITokenGenerator
    {
        public string Generate() => token;
    }

    private sealed class Sha256TokenHasher : ITokenHasher
    {
        public string Hash(string token) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
    }

    private sealed class FakeInvitationStore : IInvitationStore
    {
        public int Invalidations { get; private set; }
        public string TokenHash { get; private set; } = string.Empty;
        public Task<IssueInvitationResult> CreateAsync(Guid customerId, Guid tenantId, Guid userId, string recipient, string tokenHash, string actorId, DateTimeOffset issuedAt, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
        {
            Invalidations++;
            TokenHash = tokenHash;
            return Task.FromResult(new IssueInvitationResult(Guid.NewGuid(), string.Empty, expiresAt));
        }
    }
}
