using FinancasSaaS.Application.Abstractions;

namespace FinancasSaaS.Application.Platform.CompleteRegistration;

public sealed record CompleteRegistrationCommand(string Token, string Password);
public sealed record CompleteRegistrationResult(Guid UserId, Guid TenantId);

public interface IRegistrationCompletionStore
{
    Task<CompleteRegistrationResult?> CompleteAsync(string tokenHash, string password, DateTimeOffset now, CancellationToken cancellationToken = default);
}

public sealed class CompleteRegistrationUseCase(
    IRegistrationCompletionStore store,
    ITokenHasher tokenHasher,
    ISystemClock clock)
{
    public Task<CompleteRegistrationResult?> ExecuteAsync(CompleteRegistrationCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Token) || string.IsNullOrWhiteSpace(command.Password))
        {
            throw new ArgumentException("Token e senha são obrigatórios.", nameof(command));
        }

        return store.CompleteAsync(tokenHasher.Hash(command.Token), command.Password, clock.UtcNow, cancellationToken);
    }
}
