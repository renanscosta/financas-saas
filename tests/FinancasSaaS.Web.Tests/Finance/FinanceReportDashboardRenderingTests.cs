using System.Net;
using System.Reflection;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Application.Finance.Reports;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Web.Components.Pages.Customer.Finance;
using FinancasSaaS.Web.Components.Ui;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FinancasSaaS.Web.Tests.Finance;

public sealed class FinanceReportDashboardRenderingTests
{
    [Fact]
    public async Task ResumoMostraSaldoNegativoEAvisoBancario()
    {
        var store = new TestStore();
        var html = await RenderAsync(store);

        Assert.Contains("Saldo realizado", html, StringComparison.Ordinal);
        Assert.Contains("-R$ 30,00 (negativo)", html, StringComparison.Ordinal);
        Assert.Contains("não são o saldo da sua conta bancária", html, StringComparison.Ordinal);
        Assert.Contains(DateTime.Today.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture), html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AusenciaDeDadosNaoEhConfundidaComErro()
    {
        var html = await RenderAsync(new TestStore { Empty = true });

        Assert.Contains("Nenhum lançamento para este mês", html, StringComparison.Ordinal);
        Assert.Contains("R$ 0,00", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Não foi possível consultar", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ErroOfereceNovaTentativaSemExibirZeros()
    {
        var html = await RenderAsync(new TestStore { Failure = true });

        Assert.Contains("Tentar novamente", html, StringComparison.Ordinal);
        Assert.Contains("role=\"alert\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("R$ 0,00", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CriterioSeguePrecedenciaDeUrlSessaoEPreferencia()
    {
        var store = new TestStore { Preference = ReportDateCriterion.PaymentReceipt };
        var service = new FinanceReportService(store, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance);
        var session = new FinancasSaaS.Web.Components.ReportCriterionSession(service);
        var today = new DateOnly(2026, 10, 1);
        FinancasSaaS.Web.Components.ReportQueryParameters Parse(string query) =>
            FinancasSaaS.Web.Components.ReportQueryParameters.Parse(new Uri("https://localhost/cliente/dashboard" + query), today);

        Assert.Equal(ReportDateCriterion.PaymentReceipt, await session.ResolveAsync(Parse("")));
        Assert.Equal(ReportDateCriterion.Competence, await session.ResolveAsync(Parse("?criterio=competencia")));
        Assert.Equal(ReportDateCriterion.Competence, await session.ResolveAsync(Parse("")));
        Assert.Equal(ReportDateCriterion.PaymentReceipt, await session.ResolveAsync(Parse("?criterio=invalido")));
    }

    [Fact]
    public async Task SeletorAlteraSemSalvarEPermiteRepetirGravacao()
    {
        var response = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempts = new List<ReportDateCriterion>();
        var store = new TestStore
        {
            PreferenceResponder = criterion =>
            {
                attempts.Add(criterion);
                return attempts.Count == 1 ? response.Task : Task.CompletedTask;
            }
        };
        var selector = new DateCriterionSelector();
        var type = typeof(DateCriterionSelector);
        type.GetProperty("ReportService", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(selector,
            new FinanceReportService(store, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance));
        ReportDateCriterion? changed = null;
        type.GetProperty(nameof(DateCriterionSelector.CriterionChanged))!.SetValue(selector,
            EventCallback.Factory.Create<ReportDateCriterion>(new object(), criterion => changed = criterion));
        var change = type.GetMethod("ChangeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)change.Invoke(selector, [new ChangeEventArgs { Value = nameof(ReportDateCriterion.PaymentReceipt) }])!;
        Assert.Equal(ReportDateCriterion.PaymentReceipt, changed);
        Assert.Empty(attempts);

        type.GetProperty(nameof(DateCriterionSelector.Criterion))!.SetValue(selector, changed);
        var save = type.GetMethod("SaveAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var first = (Task)save.Invoke(selector, null)!;
        Assert.True((bool)type.GetField("IsSaving", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(selector)!);
        response.SetException(new InvalidOperationException());
        await first;
        Assert.True((bool)type.GetField("HasError", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(selector)!);
        Assert.Equal(ReportDateCriterion.Competence, store.Preference);
        await (Task)save.Invoke(selector, null)!;
        Assert.True((bool)type.GetField("Saved", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(selector)!);
        Assert.Equal(ReportDateCriterion.PaymentReceipt, store.Preference);
        Assert.Equal(new[] { ReportDateCriterion.PaymentReceipt, ReportDateCriterion.PaymentReceipt }, attempts);
    }

    [Fact]
    public async Task AlterarCriterioPreservaFiltrosELimpezaPreservaCriterio()
    {
        var category = new FinanceReportCategory(Guid.NewGuid(), "Mercado");
        var store = new TestStore { Categories = [category] };
        var service = new FinanceReportService(store, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance);
        var report = new Reports();
        var type = typeof(Reports);
        type.GetProperty("ReportService", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(report, service);
        type.GetProperty("CriterionSession", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(report,
            new FinancasSaaS.Web.Components.ReportCriterionSession(service));
        type.GetField("Start", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(report, new DateOnly(2026, 9, 1));
        type.GetField("End", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(report, new DateOnly(2026, 9, 30));
        type.GetField("CategoryId", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(report, category.Id);
        type.GetField("Status", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(report, ManualTransactionStatus.Planned);
        await (Task)type.GetMethod("ChangeCriterionAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(report, [ReportDateCriterion.PaymentReceipt])!;
        Assert.Equal(category.Id, store.LastExpensesQuery!.CategoryId);
        Assert.Equal(ManualTransactionStatus.Planned, store.LastExpensesQuery.Status);
        Assert.Equal(new DateOnly(2026, 9, 1), store.LastExpensesQuery.Period.Start);
        await (Task)type.GetMethod("ClearFiltersAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(report, null)!;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var initial = ReportPeriod.ForMonth(today.Year, today.Month);
        Assert.Equal(initial.Start, store.LastExpensesQuery!.Period.Start);
        Assert.Equal(initial.End, store.LastExpensesQuery.Period.End);
        Assert.Null(store.LastExpensesQuery.CategoryId);
        Assert.Null(store.LastExpensesQuery.Status);
        Assert.Equal(ReportDateCriterion.PaymentReceipt, store.LastExpensesQuery.Criterion);
    }

    [Theory]
    [InlineData(ReportDateCriterion.Competence, "Cada lançamento conta no mês da sua competência")]
    [InlineData(ReportDateCriterion.PaymentReceipt, "Lançamentos previstos contam na data prevista")]
    public async Task CriterioERegraPermanecemVisiveisAoNavegarEntrePaginas(ReportDateCriterion criterion, string rule)
    {
        var store = new TestStore();
        var service = new FinanceReportService(store, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance);
        var session = new FinancasSaaS.Web.Components.ReportCriterionSession(service) { Criterion = criterion };

        var dashboard = await RenderAsync(store, session);
        Assert.Contains(rule, dashboard, StringComparison.Ordinal);
        Assert.Equal(criterion, store.LastDashboardQuery!.Criterion);

        var reports = await RenderReportsAsync(store, "", session);
        Assert.Contains(rule, reports, StringComparison.Ordinal);
        Assert.Equal(criterion, store.LastExpensesQuery!.Criterion);
    }

    [Theory]
    [InlineData("?categoria=invalida", "Categoria inválida")]
    [InlineData("?inicio=2026-10-10&fim=2026-09-10", "A data inicial não pode ser posterior")]
    public async Task RelatorioInvalidoNaoConsultaDespesas(string query, string message)
    {
        var store = new TestStore();
        var html = await RenderReportsAsync(store, query);
        Assert.Contains(message, html, StringComparison.Ordinal);
        Assert.False(store.ExpensesQueried);
    }

    [Fact]
    public async Task RelatorioMostraDadosEIntervaloInicial()
    {
        var store = new TestStore();
        var html = await RenderReportsAsync(store, "");
        Assert.Contains("Data inicial", html, StringComparison.Ordinal);
        Assert.Contains("Total geral", html, StringComparison.Ordinal);
        Assert.Contains("R$ 30,00", html, StringComparison.Ordinal);
        var today = DateOnly.FromDateTime(DateTime.Today);
        Assert.Equal(ReportPeriod.ForMonth(today.Year, today.Month).Start, store.LastExpensesQuery!.Period.Start);
        Assert.Equal(ReportPeriod.ForMonth(today.Year, today.Month).End, store.LastExpensesQuery.Period.End);
        Assert.Contains("aria-live=\"polite\"", html, StringComparison.Ordinal);
        Assert.True(store.ExpensesQueried);
    }

    [Fact]
    public async Task RelatorioExibeCategoriaHistoricaEEstadosVazioEErro()
    {
        var category = new FinanceReportCategory(Guid.NewGuid(), "Categoria histórica renomeada");
        var store = new TestStore { Empty = true, Categories = [category] };
        var empty = await RenderReportsAsync(store, $"?categoria={category.Id}");
        Assert.Contains(category.Name, empty, StringComparison.Ordinal);
        Assert.Equal(category.Id, store.LastExpensesQuery!.CategoryId);
        Assert.Contains("Nenhuma despesa encontrada", empty, StringComparison.Ordinal);
        Assert.DoesNotContain("R$ 30,00", empty, StringComparison.Ordinal);
        var error = await RenderReportsAsync(new TestStore { Failure = true }, "");
        Assert.Contains("Não foi possível consultar o relatório", error, StringComparison.Ordinal);
        Assert.Contains("Tentar novamente", error, StringComparison.Ordinal);
        Assert.DoesNotContain("R$ 0,00", error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ComposicaoMostraParcelaETotalAtualizado()
    {
        var store = new TestStore
        {
            Composition = [new FinanceReportCompositionItem(Guid.NewGuid(), "Compra", ManualTransactionType.Expense,
                Guid.NewGuid(), "Mercado", 50m, ManualTransactionStatus.Planned, new DateOnly(2026, 10, 1),
                new DateOnly(2026, 10, 15), null, new DateOnly(2026, 10, 1), Guid.NewGuid(), "Compra", 1, 2)]
        };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<FinanceReportService>();
        services.AddSingleton<IFinanceReportStore>(store);
        services.AddSingleton<ICurrentUserContext, TestUser>();
        services.AddSingleton<ISystemClock, TestClock>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(ReportCompositionPanel.Query)] = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence),
            [nameof(ReportCompositionPanel.Title)] = "Despesas previstas",
            [nameof(ReportCompositionPanel.ExpectedTotal)] = 40m
        });
        var html = WebUtility.HtmlDecode(await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<ReportCompositionPanel>(parameters)).ToHtmlString()));

        Assert.Contains("parcela 1 de 2", html, StringComparison.Ordinal);
        Assert.Contains("R$ 50,00", html, StringComparison.Ordinal);
        Assert.Contains("Os dados foram atualizados", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ComposicaoVaziaMostraEstadoEAlvoDeFoco()
    {
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence);
        var html = await RenderCompositionAsync(new TestStore(), query, "Despesas previstas", 0m);

        Assert.Contains("Nenhum lançamento encontrado para este total", html, StringComparison.Ordinal);
        Assert.Contains("Total: R$ 0,00 | Quantidade: 0", html, StringComparison.Ordinal);
        Assert.Contains("<h2 id=\"composition-title\" tabindex=\"-1\">Despesas previstas</h2>", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ComposicaoLinkaLancamentosAvulsosEParcelasAsTelasDeOrigem()
    {
        var date = new DateOnly(2026, 10, 1);
        var store = new TestStore
        {
            Composition = [
                new FinanceReportCompositionItem(Guid.NewGuid(), "Conta de luz", ManualTransactionType.Expense,
                    Guid.NewGuid(), "Moradia", 25m, ManualTransactionStatus.Planned, date, date, null, date,
                    null, null, null, null),
                new FinanceReportCompositionItem(Guid.NewGuid(), "Compra parcelada", ManualTransactionType.Expense,
                    Guid.NewGuid(), "Mercado", 50m, ManualTransactionStatus.Planned, date, date, null, date,
                    Guid.NewGuid(), "Eletrodoméstico", 1, 2)]
        };
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence);
        var html = await RenderCompositionAsync(store, query, "Despesas previstas", 75m);

        Assert.Contains("href=\"/cliente/lancamentos\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/cliente/compras-parceladas\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RespostaAntigaNaoSubstituiMesMaisRecente()
    {
        var responses = new Dictionary<int, TaskCompletionSource<IReadOnlyList<FinanceReportAggregate>>>();
        var store = new TestStore
        {
            DashboardResponder = query =>
            {
                var response = new TaskCompletionSource<IReadOnlyList<FinanceReportAggregate>>(TaskCreationOptions.RunContinuationsAsynchronously);
                responses.Add(query.Period.Start.Month, response);
                return response.Task;
            }
        };
        var dashboard = new Dashboard();
        var type = typeof(Dashboard);
        type.GetProperty("ReportService", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(dashboard,
            new FinanceReportService(store, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance));
        var month = type.GetField("Month", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var load = type.GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        month.SetValue(dashboard, new DateOnly(2026, 9, 1));
        var first = (Task)load.Invoke(dashboard, null)!;
        month.SetValue(dashboard, new DateOnly(2026, 10, 1));
        var second = (Task)load.Invoke(dashboard, null)!;
        responses[9].SetResult([]);
        await first;
        Assert.True((bool)type.GetField("IsLoading", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dashboard)!);
        responses[10].SetResult([new(new DateOnly(2026, 10, 1), Guid.NewGuid(), "Mercado", ManualTransactionType.Expense, ManualTransactionStatus.Planned, 50m, 1)]);
        await second;
        var result = (FinanceDashboardReport)type.GetField("Report", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dashboard)!;
        Assert.Equal(50m, result.Summary.PlannedExpenses);
        Assert.False((bool)type.GetField("IsLoading", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dashboard)!);
    }

    [Fact]
    public async Task DashboardAnunciaCarregamentoEnquantoConsultaNaoTermina()
    {
        var response = new TaskCompletionSource<IReadOnlyList<FinanceReportAggregate>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new TestStore { DashboardResponder = _ => response.Task };
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<NavigationManager>(new TestNavigation());
        services.AddSingleton<FinanceReportService>();
        services.AddSingleton<FinancasSaaS.Web.Components.ReportCriterionSession>();
        services.AddSingleton<IFinanceReportStore>(store);
        services.AddSingleton<ICurrentUserContext, TestUser>();
        services.AddSingleton<ISystemClock, TestClock>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        var root = await renderer.Dispatcher.InvokeAsync(() => renderer.BeginRenderingComponent<Dashboard>());
        var html = WebUtility.HtmlDecode(await renderer.Dispatcher.InvokeAsync(root.ToHtmlString));
        Assert.Contains("Carregando dados financeiros", html, StringComparison.Ordinal);
        Assert.Contains("aria-live=\"polite\"", html, StringComparison.Ordinal);
        response.SetResult([]);
        await root.QuiescenceTask;
    }

    [Fact]
    public async Task ComposicaoDescartaRespostaAntigaSemEncerrarCarregamentoAtual()
    {
        var responses = new Dictionary<int, TaskCompletionSource<IReadOnlyList<FinanceReportCompositionItem>>>();
        var store = new TestStore
        {
            CompositionResponder = query =>
            {
                var response = new TaskCompletionSource<IReadOnlyList<FinanceReportCompositionItem>>(TaskCreationOptions.RunContinuationsAsynchronously);
                responses.Add(query.Period.Start.Month, response);
                return response.Task;
            }
        };
        var panel = Activator.CreateInstance<ReportCompositionPanel>();
        var type = typeof(ReportCompositionPanel);
        var queryProperty = type.GetProperty(nameof(ReportCompositionPanel.Query))!;
        queryProperty.SetValue(panel, new FinanceReportQuery(ReportPeriod.ForMonth(2026, 9), ReportDateCriterion.Competence));
        type.GetProperty(nameof(ReportCompositionPanel.Title))!.SetValue(panel, "Composição");
        type.GetProperty("ReportService", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(panel,
            new FinanceReportService(store, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance));
        var load = type.GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var first = (Task)load.Invoke(panel, null)!;
        queryProperty.SetValue(panel, new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence));
        var second = (Task)load.Invoke(panel, null)!;
        responses[9].SetResult([]);
        await first;
        Assert.True((bool)type.GetField("IsLoading", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(panel)!);
        var date = new DateOnly(2026, 10, 1);
        responses[10].SetResult([new FinanceReportCompositionItem(Guid.NewGuid(), "Atual", ManualTransactionType.Expense,
            Guid.NewGuid(), "Categoria", 10m, ManualTransactionStatus.Planned, date, date, null, date, null, null, null, null)]);
        await second;
        var result = (FinanceReportComposition)type.GetField("Composition", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(panel)!;
        Assert.Equal(10m, result.Total);
        Assert.False((bool)type.GetField("IsLoading", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(panel)!);
    }

    [Fact]
    public async Task ComposicaoRepeteConsultaComOsMesmosParametrosAposFalha()
    {
        var queries = new List<FinanceReportQuery>();
        var store = new TestStore
        {
            CompositionResponder = query =>
            {
                queries.Add(query);
                return queries.Count == 1
                    ? Task.FromException<IReadOnlyList<FinanceReportCompositionItem>>(new InvalidOperationException())
                    : Task.FromResult<IReadOnlyList<FinanceReportCompositionItem>>([]);
            }
        };
        var query = new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.PaymentReceipt,
            ManualTransactionType.Expense, ManualTransactionStatus.Planned);
        var panel = Activator.CreateInstance<ReportCompositionPanel>();
        var type = typeof(ReportCompositionPanel);
        type.GetProperty(nameof(ReportCompositionPanel.Query))!.SetValue(panel, query);
        type.GetProperty(nameof(ReportCompositionPanel.Title))!.SetValue(panel, "Despesas previstas");
        type.GetProperty("ReportService", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(panel,
            new FinanceReportService(store, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance));
        var load = type.GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await (Task)load.Invoke(panel, null)!;
        Assert.True((bool)type.GetField("HasError", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(panel)!);
        await (Task)load.Invoke(panel, null)!;
        Assert.False((bool)type.GetField("HasError", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(panel)!);
        var result = (FinanceReportComposition)type.GetField("Composition", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(panel)!;
        Assert.Empty(result.Items);
        Assert.Equal(0m, result.Total);
        Assert.Equal(2, queries.Count);
        Assert.Same(query, queries[0]);
        Assert.Same(query, queries[1]);
    }

    [Fact]
    public async Task FalhaDaComposicaoPreservaResumoDoDashboard()
    {
        var store = new TestStore
        {
            CompositionResponder = _ => Task.FromException<IReadOnlyList<FinanceReportCompositionItem>>(new InvalidOperationException())
        };
        var service = new FinanceReportService(store, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance);
        var dashboard = new Dashboard();
        var dashboardType = typeof(Dashboard);
        dashboardType.GetProperty("ReportService", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(dashboard, service);
        dashboardType.GetField("Month", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(dashboard, new DateOnly(2026, 10, 1));
        await (Task)dashboardType.GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(dashboard, null)!;
        var originalReport = dashboardType.GetField("Report", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dashboard);
        Assert.NotNull(originalReport);

        var panel = Activator.CreateInstance<ReportCompositionPanel>();
        var panelType = typeof(ReportCompositionPanel);
        panelType.GetProperty(nameof(ReportCompositionPanel.Query))!.SetValue(panel,
            new FinanceReportQuery(ReportPeriod.ForMonth(2026, 10), ReportDateCriterion.Competence,
                ManualTransactionType.Expense, ManualTransactionStatus.Realized));
        panelType.GetProperty(nameof(ReportCompositionPanel.Title))!.SetValue(panel, "Despesas realizadas");
        panelType.GetProperty("ReportService", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(panel, service);
        await (Task)panelType.GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(panel, null)!;

        Assert.True((bool)panelType.GetField("HasError", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(panel)!);
        Assert.Same(originalReport, dashboardType.GetField("Report", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dashboard));
        Assert.Equal(30m, ((FinanceDashboardReport)originalReport!).Summary.RealizedExpenses);
    }

    [Fact]
    public async Task RelatorioDescartaRespostaAntigaAoAlterarPeriodo()
    {
        var responses = new Dictionary<int, TaskCompletionSource<IReadOnlyList<FinanceReportAggregate>>>();
        var store = new TestStore
        {
            DashboardResponder = query =>
            {
                var response = new TaskCompletionSource<IReadOnlyList<FinanceReportAggregate>>(TaskCreationOptions.RunContinuationsAsynchronously);
                responses.Add(query.Period.Start.Month, response);
                return response.Task;
            }
        };
        var component = new Reports();
        var type = typeof(Reports);
        type.GetProperty("ReportService", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(component,
            new FinanceReportService(store, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance));
        var start = type.GetField("Start", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var end = type.GetField("End", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var load = type.GetMethod("LoadAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        start.SetValue(component, new DateOnly(2026, 9, 1));
        end.SetValue(component, new DateOnly(2026, 9, 30));
        var first = (Task)load.Invoke(component, null)!;
        start.SetValue(component, new DateOnly(2026, 10, 1));
        end.SetValue(component, new DateOnly(2026, 10, 31));
        var second = (Task)load.Invoke(component, null)!;
        responses[9].SetResult([]);
        await first;
        Assert.True((bool)type.GetField("IsLoading", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component)!);
        responses[10].SetResult([new(new DateOnly(2026, 10, 1), Guid.NewGuid(), "Mercado", ManualTransactionType.Expense, ManualTransactionStatus.Planned, 50m, 1)]);
        await second;
        var result = (FinanceExpensesReport)type.GetField("Report", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component)!;
        Assert.Equal(50m, result.PlannedTotal);
        Assert.False((bool)type.GetField("IsLoading", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component)!);
    }

    private static async Task<string> RenderReportsAsync(TestStore store, string query, FinancasSaaS.Web.Components.ReportCriterionSession? session = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<NavigationManager>(new TestNavigation("https://localhost/cliente/relatorios" + query));
        services.AddSingleton<FinanceReportService>();
        services.AddSingleton(session ?? new FinancasSaaS.Web.Components.ReportCriterionSession(
            new FinanceReportService(store, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance)));
        services.AddSingleton<IFinanceReportStore>(store);
        services.AddSingleton<ICurrentUserContext, TestUser>();
        services.AddSingleton<ISystemClock, TestClock>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        return WebUtility.HtmlDecode(await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<Reports>()).ToHtmlString()));
    }

    private static async Task<string> RenderCompositionAsync(TestStore store, FinanceReportQuery query, string title, decimal expectedTotal)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<FinanceReportService>();
        services.AddSingleton<IFinanceReportStore>(store);
        services.AddSingleton<ICurrentUserContext, TestUser>();
        services.AddSingleton<ISystemClock, TestClock>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
        {
            [nameof(ReportCompositionPanel.Query)] = query,
            [nameof(ReportCompositionPanel.Title)] = title,
            [nameof(ReportCompositionPanel.ExpectedTotal)] = expectedTotal
        });
        return WebUtility.HtmlDecode(await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<ReportCompositionPanel>(parameters)).ToHtmlString()));
    }

    private static async Task<string> RenderAsync(TestStore store, FinancasSaaS.Web.Components.ReportCriterionSession? session = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<NavigationManager>(new TestNavigation());
        services.AddSingleton<FinanceReportService>();
        services.AddSingleton(session ?? new FinancasSaaS.Web.Components.ReportCriterionSession(
            new FinanceReportService(store, new TestUser(), new TestClock(), NullLogger<FinanceReportService>.Instance)));
        services.AddSingleton<IFinanceReportStore>(store);
        services.AddSingleton<ICurrentUserContext, TestUser>();
        services.AddSingleton<ISystemClock, TestClock>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        return WebUtility.HtmlDecode(await renderer.Dispatcher.InvokeAsync(async () =>
            (await renderer.RenderComponentAsync<Dashboard>()).ToHtmlString()));
    }

    private sealed class TestNavigation : NavigationManager
    {
        public TestNavigation(string uri = "https://localhost/cliente/dashboard") => Initialize("https://localhost/", uri);
        protected override void NavigateToCore(string uri, bool forceLoad) => throw new NotSupportedException();
    }

    private sealed class TestUser : ICurrentUserContext
    {
        public Task<Guid?> GetUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(Guid.NewGuid());
        public Task<Guid?> GetTenantIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(Guid.NewGuid());
    }

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
    }

    private sealed class TestStore : IFinanceReportStore
    {
        public bool Empty { get; init; }
        public bool Failure { get; init; }
        public bool ExpensesQueried { get; private set; }
        public FinanceReportQuery? LastDashboardQuery { get; private set; }
        public FinanceReportQuery? LastExpensesQuery { get; private set; }
        public ReportDateCriterion Preference { get; set; } = ReportDateCriterion.Competence;
        public IReadOnlyList<FinanceReportCompositionItem> Composition { get; init; } = [];
        public IReadOnlyList<FinanceReportCategory> Categories { get; init; } = [];
        public Func<FinanceReportQuery, Task<IReadOnlyList<FinanceReportAggregate>>>? DashboardResponder { get; init; }
        public Func<FinanceReportQuery, Task<IReadOnlyList<FinanceReportCompositionItem>>>? CompositionResponder { get; init; }
        public Func<ReportDateCriterion, Task>? PreferenceResponder { get; init; }
        public Task<IReadOnlyList<FinanceReportAggregate>> GetDashboardAsync(FinanceReportQuery query, CancellationToken cancellationToken = default)
        {
            LastDashboardQuery = query;
            if (DashboardResponder is not null) return DashboardResponder(query);
            if (Failure) throw new InvalidOperationException();
            IReadOnlyList<FinanceReportAggregate> result = Empty ? [] :
            [new(query.Period.Start, Guid.NewGuid(), "Mercado", ManualTransactionType.Expense, ManualTransactionStatus.Realized, 30m, 1)];
            return Task.FromResult(result);
        }
        public async Task<IReadOnlyList<FinanceReportAggregate>> GetExpensesAsync(FinanceReportQuery query, CancellationToken cancellationToken = default)
        {
            ExpensesQueried = true;
            LastExpensesQuery = query;
            return await GetDashboardAsync(query, cancellationToken);
        }
        public Task<IReadOnlyList<FinanceReportCompositionItem>> GetCompositionAsync(FinanceReportQuery query, CancellationToken cancellationToken = default) =>
            CompositionResponder?.Invoke(query) ?? Task.FromResult(Composition);
        public Task<IReadOnlyList<FinanceReportCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default) => Task.FromResult(Categories);
        public Task<bool> CategoryExistsAsync(Guid categoryId, CancellationToken cancellationToken = default) => Task.FromResult(Categories.Any(category => category.Id == categoryId));
        public Task<FinanceReportPreference> GetPreferenceAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(new FinanceReportPreference(Preference, true));
        public async Task SavePreferenceAsync(Guid userId, ReportDateCriterion criterion, DateTimeOffset updatedAtUtc, CancellationToken cancellationToken = default)
        {
            if (PreferenceResponder is null)
            {
                throw new NotSupportedException();
            }

            await PreferenceResponder(criterion);
            Preference = criterion;
        }
    }
}