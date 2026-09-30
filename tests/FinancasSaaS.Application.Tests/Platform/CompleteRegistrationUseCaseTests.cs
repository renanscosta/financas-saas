using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Platform.CompleteRegistration;
using Xunit;

namespace FinancasSaaS.Application.Tests.Platform;

public sealed class CompleteRegistrationUseCaseTests
{
    [Fact]
    public async Task TokenInvalidoNaoHabilitaAcesso()
    {
        var store = new FakeStore();
        var useCase = new CompleteRegistrationUseCase(store, new FixedHasher(), new FixedClock());

        var result = await useCase.ExecuteAsync(new CompleteRegistrationCommand("invalid", "StrongPassword123!"));

        Assert.Null(result);
        Assert.Equal("hashed:invalid", store.TokenHash);
    }

    private sealed class FakeStore : IRegistrationCompletionStore
    {
        public string? TokenHash { get; private set; }
        public Task<CompleteRegistrationResult?> CompleteAsync(string tokenHash, string password, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            TokenHash = tokenHash;
            return Task.FromResult<CompleteRegistrationResult?>(null);
        }
    }

    private sealed class FixedHasher : ITokenHasher
    {
        public string Hash(string token) => $"hashed:{token}";
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    }
}
