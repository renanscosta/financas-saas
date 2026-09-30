using FinancasSaaS.Application.Abstractions;

namespace FinancasSaaS.Application.Platform.RegisterCustomer;

public sealed record RegisterCustomerCommand(string Email, string ActorId);
public sealed record RegisterCustomerResult(Guid CustomerId, Guid TenantId, bool AlreadyExists);

public interface ICustomerRegistrationStore
{
    Task<RegisterCustomerResult?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<RegisterCustomerResult> CreateAsync(string email, string actorId, CancellationToken cancellationToken = default);
}

public sealed class RegisterCustomerUseCase(ICustomerRegistrationStore store)
{
    public async Task<RegisterCustomerResult> ExecuteAsync(RegisterCustomerCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Email))
        {
            throw new ArgumentException("O e-mail do cliente é obrigatório.", nameof(command));
        }

        var normalizedEmail = command.Email.Trim().ToUpperInvariant();
        var existing = await store.FindByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            return existing with { AlreadyExists = true };
        }

        var result = await store.CreateAsync(normalizedEmail, command.ActorId, cancellationToken);
        return result;
    }
}
