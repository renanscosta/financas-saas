using FinancasSaaS.Application.Abstractions;

namespace FinancasSaaS.Application.Platform.ProvisionTenant;

public sealed class ProvisionTenantUseCase(ITenantProvisioner provisioner)
{
    public Task ExecuteAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException("O tenant é obrigatório.", nameof(tenantId));
        }

        return provisioner.ProvisionAsync(tenantId, cancellationToken);
    }
}
