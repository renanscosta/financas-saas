using FinancasSaaS.Web.Components.Shared;
using FinancasSaaS.Web.Components.Ui;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FinancasSaaS.Web.Tests.Ui;

public sealed class OperationFeedbackRenderingTests
{
    [Theory]
    [InlineData(OperationFeedbackState.Loading, "status", "loading")]
    [InlineData(OperationFeedbackState.Success, "status", "success")]
    [InlineData(OperationFeedbackState.Error, "alert", "error")]
    [InlineData(OperationFeedbackState.Validation, "alert", "validation")]
    public async Task EstadoTemSemanticaETextoAcessiveis(OperationFeedbackState state, string role, string stateName)
    {
        var html = await RenderAsync(state, "Mensagem de teste");

        Assert.Contains($"role=\"{role}\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains($"data-state=\"{stateName}\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aria-live=", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Mensagem de teste", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EstadoOciosoSemMensagemNaoGeraRegiaoVazia()
    {
        var html = await RenderAsync(OperationFeedbackState.Idle, null);

        Assert.DoesNotContain("role=\"status\"", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("role=\"alert\"", html, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> RenderAsync(OperationFeedbackState state, string? message)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        await using var provider = services.BuildServiceProvider();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(provider, loggerFactory);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(OperationFeedback.State)] = state,
                [nameof(OperationFeedback.Message)] = message
            });
            var component = await renderer.RenderComponentAsync<OperationFeedback>(parameters);
            return component.ToHtmlString();
        });
    }
}
