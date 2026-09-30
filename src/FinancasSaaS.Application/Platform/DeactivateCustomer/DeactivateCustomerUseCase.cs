using FinancasSaaS.Application.Abstractions;

namespace FinancasSaaS.Application.Platform.DeactivateCustomer;

public sealed record DeactivateCustomerCommand(Guid CustomerId, string ActorId, string Reason);
public sealed record CustomerDeactivationResult(Guid CustomerId, Guid TenantId);

public interface ICustomerDeactivationStore
{
    Task<CustomerDeactivationResult?> DeactivateAsync(
        Guid customerId,
        string actorId,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}

public sealed class DeactivateCustomerUseCase(
    ICustomerDeactivationStore store,
    ISystemClock clock)
{
    public async Task<CustomerDeactivationResult?> ExecuteAsync(
        DeactivateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CustomerId == Guid.Empty || string.IsNullOrWhiteSpace(command.ActorId) || string.IsNullOrWhiteSpace(command.Reason))
        {
            throw new ArgumentException("Cliente, ator e motivo são obrigatórios.", nameof(command));
        }

        var result = await store.DeactivateAsync(
            command.CustomerId,
            command.ActorId,
            command.Reason.Trim(),
            clock.UtcNow,
            cancellationToken);
        if (result is null)
        {
            return null;
        }

        return result;
    }
}