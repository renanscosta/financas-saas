using System.Reflection;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance.InstallmentPurchases;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Web.Components.Pages.Customer.Finance;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinancasSaaS.Web.Tests.Finance;

public sealed class InstallmentPurchaseHistoryConcurrencyTests
{
    [Fact]
    public async Task RespostaAntigaNaoSubstituiHistoricoDaCompraAtualmenteAberta()
    {
        var firstPurchaseId = Guid.NewGuid();
        var secondPurchaseId = Guid.NewGuid();
        var thirdPurchaseId = Guid.NewGuid();
        var store = new ControlledHistoryStore();
        var service = new InstallmentPurchaseService(
            store,
            new TestCurrentUserContext(),
            new TestSystemClock(),
            NullLogger<InstallmentPurchaseService>.Instance);
        var component = new InstallmentPurchases();
        SetProperty(component, "PurchaseService", service);
        SetProperty(component, "Logger", NullLogger<InstallmentPurchases>.Instance);

        var firstRequest = InvokeHistoryToggle(component, firstPurchaseId);
        var secondRequest = InvokeHistoryToggle(component, secondPurchaseId);

        store.Responses[firstPurchaseId].SetResult([CreateAuditEvent(firstPurchaseId)]);
        await firstRequest;
        Assert.True(GetField<bool>(component, "IsHistoryLoading"));
        Assert.Equal(secondPurchaseId, GetField<Guid?>(component, "HistoryId"));

        var thirdRequest = InvokeHistoryToggle(component, thirdPurchaseId);
        var thirdEvent = CreateAuditEvent(thirdPurchaseId);
        store.Responses[thirdPurchaseId].SetResult([thirdEvent]);
        await thirdRequest;

        var secondEvent = CreateAuditEvent(secondPurchaseId);
        store.Responses[secondPurchaseId].SetResult([secondEvent]);
        await secondRequest;

        Assert.Equal(thirdPurchaseId, GetField<Guid?>(component, "HistoryId"));
        Assert.Equal([thirdEvent], GetField<IReadOnlyList<InstallmentPurchaseAuditEvent>>(component, "History"));
        Assert.False(GetField<bool>(component, "IsHistoryLoading"));
    }

    private static Task InvokeHistoryToggle(InstallmentPurchases component, Guid purchaseId)
    {
        var method = typeof(InstallmentPurchases).GetMethod(
            "ToggleHistoryAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("O manipulador do histórico não foi encontrado.");
        return (Task)(method.Invoke(component, [purchaseId])
            ?? throw new InvalidOperationException("O manipulador do histórico não retornou uma tarefa."));
    }

    private static InstallmentPurchaseAuditEvent CreateAuditEvent(Guid purchaseId) =>
        new(Guid.NewGuid(), purchaseId, "Creation", [], DateTimeOffset.UtcNow, Guid.NewGuid(), string.Empty);

    private static void SetProperty<T>(InstallmentPurchases component, string name, T value)
    {
        var property = typeof(InstallmentPurchases).GetProperty(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"A propriedade {name} não foi encontrada.");
        property.SetValue(component, value);
    }

    private static T GetField<T>(InstallmentPurchases component, string name)
    {
        var field = typeof(InstallmentPurchases).GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"O campo {name} não foi encontrado.");
        return (T)field.GetValue(component)!;
    }

    private sealed class ControlledHistoryStore : IInstallmentPurchaseStore
    {
        public Dictionary<Guid, TaskCompletionSource<IReadOnlyList<InstallmentPurchaseAuditEvent>>> Responses { get; } = [];

        public Task<IReadOnlyList<InstallmentPurchaseAuditEvent>> GetHistoryAsync(
            Guid purchaseId,
            CancellationToken cancellationToken = default)
        {
            var response = new TaskCompletionSource<IReadOnlyList<InstallmentPurchaseAuditEvent>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            Responses.Add(purchaseId, response);
            return response.Task;
        }

        public Task<InstallmentPurchaseRecord> CreateAsync(
            InstallmentPurchase purchase,
            InstallmentPurchaseAuditEvent auditEvent,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<InstallmentPurchaseRecord?> GetAsync(
            Guid purchaseId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<InstallmentPurchaseRecord>> ListAsync(
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<InstallmentPurchaseRecord?> RealizeAsync(
            Guid purchaseId,
            int installmentNumber,
            DateOnly effectiveDate,
            long expectedVersion,
            DateTimeOffset occurredAt,
            Guid actorId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<InstallmentPurchaseRecord?> EditAsync(
            Guid purchaseId,
            string description,
            Guid categoryId,
            decimal totalAmount,
            DateOnly? firstEligibleDueDate,
            DateOnly operationDate,
            string reason,
            long expectedVersion,
            DateTimeOffset occurredAt,
            Guid actorId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<InstallmentPurchaseRecord?> CancelAsync(
            Guid purchaseId,
            DateOnly operationDate,
            string reason,
            long expectedVersion,
            DateTimeOffset occurredAt,
            Guid actorId,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestCurrentUserContext : ICurrentUserContext
    {
        public Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(Guid.NewGuid());

        public Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(Guid.NewGuid());
    }

    private sealed class TestSystemClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}
