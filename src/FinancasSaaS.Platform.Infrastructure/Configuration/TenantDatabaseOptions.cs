using Npgsql;

namespace FinancasSaaS.Platform.Infrastructure.Configuration;

public sealed class TenantDatabaseOptions
{
    public string PlatformConnectionString { get; set; } = string.Empty;
    public Dictionary<string, string> Connections { get; set; } = new(StringComparer.Ordinal);

    public string? GetConnectionString(string databaseName)
    {
        if (Connections.TryGetValue(databaseName, out var connectionString))
        {
            return connectionString;
        }

        if (string.IsNullOrWhiteSpace(PlatformConnectionString))
        {
            return null;
        }

        var builder = new NpgsqlConnectionStringBuilder(PlatformConnectionString)
        {
            Database = databaseName
        };
        return builder.ConnectionString;
    }
}
