using FinancasSaaS.Domain.Platform;
using Xunit;

namespace FinancasSaaS.Domain.Tests.Platform;

public sealed class PlatformStateTests
{
    [Fact]
    public void ClienteNovoIniciaAtivo()
    {
        var customer = new Customer(Guid.NewGuid(), "cliente@example.com", DateTimeOffset.UtcNow);

        Assert.Equal(CustomerStatus.Active, customer.Status);
    }

    [Fact]
    public void TenantNovoNaoEstaPronto()
    {
        var tenant = new Tenant(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(ProvisioningStatus.Pending, tenant.ProvisioningStatus);
    }

    [Fact]
    public void TenantProntoNaoPodeSerProvisionadoNovamente()
    {
        var tenant = new Tenant(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow);
        tenant.MarkReady();

        Assert.Throws<InvalidOperationException>(() => tenant.StartProvisioning());
    }
}
