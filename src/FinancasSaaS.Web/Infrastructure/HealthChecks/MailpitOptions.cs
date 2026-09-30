namespace FinancasSaaS.Web.Infrastructure.HealthChecks;

public sealed class MailpitOptions
{
    public string Host { get; set; } = "localhost";
    public int UiPort { get; set; } = 8025;
}