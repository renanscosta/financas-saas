using Npgsql;

namespace FinancasSaaS.IntegrationTests.Infrastructure;

public static class PostgresFixture
{
    public static string PlatformConnectionString => IntegrationTestConnections.Platform;

    public static string GetTenantConnectionString(Guid tenantId) =>
        IntegrationTestConnections.GetTenant(tenantId);

    public static async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
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
