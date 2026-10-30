using FinancasSaaS.Web.Components.Ui;
using System.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FinancasSaaS.Web.Tests.Finance;

public sealed class TransactionFeedbackRenderingTests
{
    [Fact]
    public async Task SucessoDaGravacaoPermaneceVisivelQuandoFalhaAtualizacaoDaLista()
    {
        var html = WebUtility.HtmlDecode(await RenderAsync<TransactionMutationFeedback>(new Dictionary<string, object?>
        {
            [nameof(TransactionMutationFeedback.MutationState)] = OperationFeedbackState.Success,
            [nameof(TransactionMutationFeedback.MutationMessage)] = "Lançamento registrado como previsto.",
            [nameof(TransactionMutationFeedback.RefreshState)] = OperationFeedbackState.Error,
            [nameof(TransactionMutationFeedback.RefreshMessage)] = "Não foi possível consultar os lançamentos."
        }));

        Assert.Contains("data-state=\"success\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Lançamento registrado como previsto.", html, StringComparison.Ordinal);
        Assert.Contains("data-state=\"error\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Não foi possível consultar os lançamentos.", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AusenciaDeParcelasElegiveisExibeFeedbackDeValidacao()
    {
        var html = WebUtility.HtmlDecode(await RenderAsync<TransactionMutationFeedback>(new Dictionary<string, object?>
        {
            [nameof(TransactionMutationFeedback.MutationState)] = OperationFeedbackState.Validation,
            [nameof(TransactionMutationFeedback.MutationMessage)] = "Não há parcelas futuras previstas para alterar."
        }));

        Assert.Contains("data-state=\"validation\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Não há parcelas futuras previstas para alterar.", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SucessoDaMutacaoDeCategoriaPermaneceSeparadoDoErroDeAtualizacao()
    {
        var html = WebUtility.HtmlDecode(await RenderAsync<CategoryMutationFeedback>(new Dictionary<string, object?>
        {
            [nameof(CategoryMutationFeedback.MutationState)] = OperationFeedbackState.Success,
            [nameof(CategoryMutationFeedback.MutationMessage)] = "Categoria criada.",
            [nameof(CategoryMutationFeedback.RefreshState)] = OperationFeedbackState.Error,
            [nameof(CategoryMutationFeedback.RefreshMessage)] = "Não foi possível atualizar a lista de categorias."
        }));

        Assert.Contains("Categoria criada.", html, StringComparison.Ordinal);
        Assert.Contains("Não foi possível atualizar a lista de categorias.", html, StringComparison.Ordinal);
        Assert.Contains("data-state=\"success\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("data-state=\"error\"", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FalhaAoConsultarHistoricoNaoExibeResultadoVazio()
    {
        var html = WebUtility.HtmlDecode(await RenderAsync<TransactionHistoryPanel>(new Dictionary<string, object?>
        {
            [nameof(TransactionHistoryPanel.Events)] = Array.Empty<FinancasSaaS.Domain.Finance.ManualTransactionAuditEvent>(),
            [nameof(TransactionHistoryPanel.ErrorMessage)] = "Não foi possível consultar o histórico do lançamento."
        }));

        Assert.Contains("Não foi possível consultar o histórico do lançamento.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Não há correções ou cancelamentos registrados.", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConsultaBemSucedidaSemEventosExibeResultadoVazio()
    {
        var html = WebUtility.HtmlDecode(await RenderAsync<TransactionHistoryPanel>(new Dictionary<string, object?>
        {
            [nameof(TransactionHistoryPanel.Events)] = Array.Empty<FinancasSaaS.Domain.Finance.ManualTransactionAuditEvent>()
        }));

        Assert.Contains("Não há correções ou cancelamentos registrados.", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Não foi possível consultar o histórico", html, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync<TComponent>(IDictionary<string, object?> parameters)
        where TComponent : IComponent
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(provider, loggerFactory);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<TComponent>(ParameterView.FromDictionary(parameters));
            return component.ToHtmlString();
        });
    }
}
