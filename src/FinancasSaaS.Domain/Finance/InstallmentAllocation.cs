namespace FinancasSaaS.Domain.Finance;

public static class InstallmentAllocation
{
    public const int MinimumInstallments = 2;
    public const int MaximumInstallments = 1200;

    public static IReadOnlyList<decimal> Allocate(decimal total, int installmentCount)
    {
        ManualTransaction.ValidateAmount(total);
        if (installmentCount is < MinimumInstallments or > MaximumInstallments)
        {
            throw new ArgumentOutOfRangeException(
                nameof(installmentCount),
                $"A quantidade deve estar entre {MinimumInstallments} e {MaximumInstallments} parcelas.");
        }

        var totalCents = decimal.ToInt64(total * 100m);
        if (totalCents < installmentCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(total),
                "O valor total não permite uma parcela positiva para cada vencimento.");
        }

        var quotient = totalCents / installmentCount;
        var remainder = totalCents % installmentCount;
        var amounts = new decimal[installmentCount];
        for (var index = 0; index < installmentCount; index++)
        {
            var cents = quotient + (index < remainder ? 1 : 0);
            amounts[index] = cents / 100m;
        }

        return amounts;
    }
}
