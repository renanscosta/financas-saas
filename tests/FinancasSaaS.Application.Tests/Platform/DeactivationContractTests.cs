using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Platform.DeactivateCustomer;
using Xunit;

namespace FinancasSaaS.Application.Tests.Platform;

public sealed class DeactivationContractTests
{
    [Fact]
    public async Task DesativacaoBloqueiaAcessoInvalidaConvitesEAuditaMotivo()
    {
        var store = new RecordingStore();
        var tenantId = Guid.NewGuid();
        store.TenantId = tenantId;
        var useCase = new DeactivateCustomerUseCase(store, new FixedClock());
        var customerId = Guid.NewGuid();

        var result = await useCase.ExecuteAsync(new DeactivateCustomerCommand(
            customerId,
            "operator-1",
            "Solicitação do cliente"));

        Assert.NotNull(result);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal(customerId, store.CustomerId);
        Assert.Equal("operator-1", store.ActorId);
        Assert.Equal("Solicitação do cliente", store.Reason);
        Assert.True(store.AccessBlocked);
        Assert.True(store.InvitationsInvalidated);
    }

    private sealed class RecordingStore : ICustomerDeactivationStore
    {
        public Guid CustomerId { get; private set; }
        public Guid TenantId { get; set; }
        public string? ActorId { get; private set; }
        public string? Reason { get; private set; }
        public bool AccessBlocked { get; private set; }
        public bool InvitationsInvalidated { get; private set; }

        public Task<CustomerDeactivationResult?> DeactivateAsync(Guid customerId, string actorId, string reason, DateTimeOffset now, CancellationToken cancellationToken = default)
        {
            CustomerId = customerId;
            ActorId = actorId;
            Reason = reason;
            AccessBlocked = true;
            InvitationsInvalidated = true;
            return Task.FromResult<CustomerDeactivationResult?>(new CustomerDeactivationResult(customerId, TenantId));
        }
    }

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    }
}