namespace FinancasSaaS.Platform.Infrastructure.Configuration;

public sealed class EmailOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string From { get; set; } = "no-reply@financas.local";
}
