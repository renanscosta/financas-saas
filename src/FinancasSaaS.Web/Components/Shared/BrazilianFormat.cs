using System.Globalization;

namespace FinancasSaaS.Web.Components;

public static class BrazilianFormat
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("pt-BR");

    public static string Money(decimal amount)
    {
        var formatted = decimal.Abs(amount).ToString("N2", Culture);
        return amount < 0 ? $"-R$ {formatted}" : $"R$ {formatted}";
    }

    public static string Balance(decimal amount) =>
        amount < 0 ? $"{Money(amount)} (negativo)" : Money(amount);

    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy", Culture);

    public static string Month(DateOnly date) => date.ToString("MMMM 'de' yyyy", Culture);
}