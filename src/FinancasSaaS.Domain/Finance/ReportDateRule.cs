namespace FinancasSaaS.Domain.Finance;

public static class ReportDateRule
{
    public static DateOnly? GetReferenceDate(
        ReportDateCriterion criterion,
        ManualTransactionStatus status,
        DateOnly competenceDate,
        DateOnly expectedDate,
        DateOnly? effectiveDate)
    {
        if (criterion is not (ReportDateCriterion.Competence or ReportDateCriterion.PaymentReceipt))
        {
            throw new ArgumentOutOfRangeException(nameof(criterion));
        }

        if (status == ManualTransactionStatus.Cancelled)
        {
            return null;
        }

        if (status is not (ManualTransactionStatus.Planned or ManualTransactionStatus.Realized))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (status == ManualTransactionStatus.Realized && effectiveDate is null)
        {
            throw new ArgumentException("O lançamento realizado deve possuir data efetiva.", nameof(effectiveDate));
        }

        return criterion == ReportDateCriterion.Competence
            ? competenceDate
            : status == ManualTransactionStatus.Planned ? expectedDate : effectiveDate;
    }
}