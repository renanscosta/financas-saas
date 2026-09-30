namespace FinancasSaaS.Application.Platform.GetProvisioningStatus;

public sealed record ProvisioningStatusResult(
    Guid TenantId,
    string Status,
    int Attempt,
    string? LastFailure,
    DateTimeOffset LastAttemptAt);

public interface IProvisioningStatusReader
{
    Task<ProvisioningStatusResult?> ReadAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed class GetProvisioningStatusUseCase(IProvisioningStatusReader reader)
{
    public Task<ProvisioningStatusResult?> ExecuteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("O tenant é obrigatório.", nameof(tenantId));
        }

        return reader.ReadAsync(tenantId, cancellationToken);
    }
}
