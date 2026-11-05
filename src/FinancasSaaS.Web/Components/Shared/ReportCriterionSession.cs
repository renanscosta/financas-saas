using FinancasSaaS.Application.Finance.Reports;
using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Web.Components;

public sealed class ReportCriterionSession(FinanceReportService service)
{
    public ReportDateCriterion? Criterion { get; set; }

    public async Task<ReportDateCriterion> ResolveAsync(ReportQueryParameters parameters)
    {
        var criterion = parameters.InvalidCriterion
            ? (await service.GetPreferenceAsync()).Criterion
            : parameters.Criterion ?? Criterion ?? (await service.GetPreferenceAsync()).Criterion;
        Criterion = criterion;
        return criterion;
    }
}