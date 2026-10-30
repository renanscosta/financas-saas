using Npgsql;

namespace FinancasSaaS.IntegrationTests.Infrastructure;

public static class IntegrationTestConnections
{
    public static string Platform => Environment.GetEnvironmentVariable("PLATFORM_CONNECTION_STRING")
        ?? throw new InvalidOperationException(
            "Defina PLATFORM_CONNECTION_STRING para executar os testes PostgreSQL de integração.");

    public static string GetTenant(Guid tenantId)
    {
        var builder = new NpgsqlConnectionStringBuilder(Platform)
        {
            Database = $"financas_test_tenant_{tenantId:N}"
        };
        return builder.ConnectionString;
    }

    public static string GetAdmin()
    {
        var builder = new NpgsqlConnectionStringBuilder(Platform)
        {
            Database = "postgres"
        };
        return builder.ConnectionString;
    }
}
