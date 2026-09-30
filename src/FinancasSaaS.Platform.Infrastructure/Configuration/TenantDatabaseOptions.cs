namespace FinancasSaaS.Platform.Infrastructure.Configuration;

public sealed class TenantDatabaseOptions
{
    public string PlatformConnectionString { get; set; } = string.Empty;
    public Dictionary<string, string> Connections { get; set; } = new(StringComparer.Ordinal);
}
