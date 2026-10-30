using System.Net;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Web.Components.Ui;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinancasSaaS.Web.Tests.Finance;

public sealed class InstallmentPurchaseHistoryTests
{
    [Fact]
    public async Task ExibeEventoComAtorInstanteMotivoEAlteracoesDasParcelas()
    {
        var actorId = Guid.NewGuid();
        var changes = new[]
        {
            new InstallmentPurchaseAuditChange(2, "Amount", "33.33", "38.33"),
            new InstallmentPurchaseAuditChange(2, "DueDate", "2026-03-31", "2026-04-30")
        };
        var events = new[]
        {
            new InstallmentPurchaseAuditEvent(
                Guid.NewGuid(), Guid.NewGuid(), "Creation", changes, new DateTimeOffset(2026, 1, 1, 10, 30, 0, TimeSpan.Zero), actorId, string.Empty),
            new InstallmentPurchaseAuditEvent(
                Guid.NewGuid(), Guid.NewGuid(), "Realization", changes, new DateTimeOffset(2026, 1, 2, 10, 30, 0, TimeSpan.Zero), actorId, string.Empty),
            new InstallmentPurchaseAuditEvent(
                Guid.NewGuid(), Guid.NewGuid(), "Edit", changes, new DateTimeOffset(2026, 2, 1, 10, 30, 0, TimeSpan.Zero), actorId, "Ajuste de orçamento"),
            new InstallmentPurchaseAuditEvent(
                Guid.NewGuid(), Guid.NewGuid(), "Cancellation", changes, new DateTimeOffset(2026, 3, 1, 10, 30, 0, TimeSpan.Zero), actorId, "Compra encerrada")
        };

        var html = WebUtility.HtmlDecode(await RenderAsync(new Dictionary<string, object?>
        {
            [nameof(InstallmentPurchaseHistoryPanel.Events)] = events
        }));

        Assert.Contains("Criação", html, StringComparison.Ordinal);
        Assert.Contains("Realização", html, StringComparison.Ordinal);
        Assert.Contains("Edição", html, StringComparison.Ordinal);
        Assert.Contains("Cancelamento", html, StringComparison.Ordinal);
        Assert.Contains(actorId.ToString(), html, StringComparison.Ordinal);
        Assert.Contains("01/02/2026 10:30:00 UTC", html, StringComparison.Ordinal);
        Assert.Contains("Ajuste de orçamento", html, StringComparison.Ordinal);
        Assert.Contains("Compra encerrada", html, StringComparison.Ordinal);
        Assert.Contains("Parcela 2", html, StringComparison.Ordinal);
        Assert.Contains("Amount:", html, StringComparison.Ordinal);
        Assert.Contains("33.33", html, StringComparison.Ordinal);
        Assert.Contains("38.33", html, StringComparison.Ordinal);
        Assert.Contains("DueDate:", html, StringComparison.Ordinal);
        Assert.Contains("2026-03-31", html, StringComparison.Ordinal);
        Assert.Contains("2026-04-30", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FalhaDeHistoricoNaoEApresentadaComoResultadoVazio()
    {
        var html = WebUtility.HtmlDecode(await RenderAsync(new Dictionary<string, object?>
        {
            [nameof(InstallmentPurchaseHistoryPanel.Events)] = Array.Empty<InstallmentPurchaseAuditEvent>(),
            [nameof(InstallmentPurchaseHistoryPanel.ErrorMessage)] = "Não foi possível consultar o histórico do parcelamento."
        }));

        Assert.Contains("Não foi possível consultar o histórico do parcelamento.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Nenhum evento de auditoria foi encontrado.", html, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync(IDictionary<string, object?> parameters)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(provider, loggerFactory);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<InstallmentPurchaseHistoryPanel>(
                ParameterView.FromDictionary(parameters));
            return component.ToHtmlString();
        });
    }
}
