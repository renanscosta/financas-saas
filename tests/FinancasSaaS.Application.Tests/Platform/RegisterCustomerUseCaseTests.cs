using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Platform.RegisterCustomer;
using Xunit;

namespace FinancasSaaS.Application.Tests.Platform;

public sealed class RegisterCustomerUseCaseTests
{
    [Fact]
    public async Task RetornaClienteExistenteSemDuplicar()
    {
        var expected = new RegisterCustomerResult(Guid.NewGuid(), Guid.NewGuid(), false);
        var store = new FakeStore(expected);
        var useCase = new RegisterCustomerUseCase(store);

        var result = await useCase.ExecuteAsync(new RegisterCustomerCommand("cliente@example.com", "admin"));

        Assert.Equal(expected.CustomerId, result.CustomerId);
        Assert.True(result.AlreadyExists);
        Assert.Equal(0, store.CreateCalls);
    }

    private sealed class FakeStore(RegisterCustomerResult existing) : ICustomerRegistrationStore
    {
        public int CreateCalls { get; private set; }
        public Task<RegisterCustomerResult?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<RegisterCustomerResult?>(existing);
        public Task<RegisterCustomerResult> CreateAsync(string email, string actorId, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            return Task.FromResult(existing);
        }
    }

}
