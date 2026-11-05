namespace FinancasSaaS.Domain.Finance;

public sealed record FinancialSummary(
    decimal PlannedIncome,
    decimal RealizedIncome,
    decimal PlannedExpenses,
    decimal RealizedExpenses)
{
    public decimal PlannedBalance => PlannedIncome - PlannedExpenses;
    public decimal RealizedBalance => RealizedIncome - RealizedExpenses;
}