using FinancasSaaS.Domain.Platform;
using Xunit;

namespace FinancasSaaS.Domain.Tests.Platform;

public sealed class CustomerDeactivationTests
{
    [Fact]
    public void ClienteAtivoPodeSerDesativadoSemPerderSuaIdentidade()
    {
        var customerId = Guid.NewGuid();
        var customer = new Customer(customerId, "cliente@example.com", DateTimeOffset.UtcNow);

        customer.Disable();

        Assert.Equal(CustomerStatus.Disabled, customer.Status);
        Assert.Equal(customerId, customer.Id);
        Assert.Equal("cliente@example.com", customer.Email);
    }
}