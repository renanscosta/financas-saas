using Npgsql;

namespace FinancasSaaS.IntegrationTests.Infrastructure;

public sealed class PostgresFixture
{
    public string PlatformConnectionString { get; } =
        Environment.GetEnvironmentVariable("PLATFORM_CONNECTION_STRING")
        ?? "Host=localhost;Port=5432;Database=financas_platform;Username=financas;Password=local-only-change-me";

    public static string GetTenantConnectionString(Guid tenantId) =>
        $"Host=localhost;Port=5432;Database=financas_tenant_{tenantId:N};Username=financas;Password=local-only-change-me";

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(PlatformConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch (NpgsqlException)
        {
            return false;
        }
    }
}
