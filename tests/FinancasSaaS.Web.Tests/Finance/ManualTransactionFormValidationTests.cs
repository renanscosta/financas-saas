using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using FinancasSaaS.Domain.Finance;
using FinancasSaaS.Web.Components.Pages.Customer.Finance;
using FinancasSaaS.Web.Components.Ui;

namespace FinancasSaaS.Web.Tests.Finance;

public sealed class ManualTransactionFormValidationTests
{
    [Fact]
    public async Task DataPrevistaAusenteRetornaValidacaoSemChamarServico()
    {
        var component = new Transactions();
        var componentType = typeof(Transactions);
        var form = componentType.GetField("CreateForm", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component)!;
        var formType = form.GetType();
        formType.GetProperty("CategoryId")!.SetValue(form, Guid.NewGuid());
        formType.GetProperty("CompetenceDate")!.SetValue(form, new DateOnly(2026, 11, 5));
        formType.GetProperty("ExpectedDate")!.SetValue(form, null);

        await (Task)componentType.GetMethod("CreateAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(component, null)!;

        Assert.Equal("Informe categoria, competência e data prevista.",
            componentType.GetField("Message", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component));
        Assert.Equal(OperationFeedbackState.Validation,
            componentType.GetField("FeedbackState", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(component));
    }

    [Fact]
    public void LimiteDecimalEhValidadoSobCulturaBrasileira()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var brazilianCulture = CultureInfo.GetCultureInfo("pt-BR");
            CultureInfo.CurrentCulture = brazilianCulture;
            CultureInfo.CurrentUICulture = brazilianCulture;

            var formType = typeof(Transactions).GetNestedType("CreateFormModel", BindingFlags.NonPublic)!;
            var form = Activator.CreateInstance(formType, nonPublic: true)!;
            formType.GetProperty("Description")!.SetValue(form, "Despesa de teste");
            formType.GetProperty("Amount")!.SetValue(form, 0.01m);
            var validationResults = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(form, new ValidationContext(form), validationResults, validateAllProperties: true);

            Assert.True(isValid, string.Join("; ", validationResults.Select(result => result.ErrorMessage)));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
