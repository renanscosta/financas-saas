using FinancasSaaS.Domain.Finance;

namespace FinancasSaaS.Finance.Infrastructure.Persistence.Entities;

public sealed class FinanceReportPreferenceEntity
{
    public Guid UserId { get; set; }
    public ReportDateCriterion Criterion { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid UpdatedByUserId { get; set; }
}