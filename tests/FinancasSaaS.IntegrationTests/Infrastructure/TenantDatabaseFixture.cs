using FinancasSaaS.Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinancasSaaS.IntegrationTests.Infrastructure;

public sealed class TenantDatabaseFixture : IAsyncLifetime
{
    private readonly string _adminConnectionString;

    public Guid FirstTenantId { get; } = Guid.NewGuid();
    public Guid SecondTenantId { get; } = Guid.NewGuid();
    public string FirstConnectionString => GetConnectionString(FirstTenantId);
    public string SecondConnectionString => GetConnectionString(SecondTenantId);

    public TenantDatabaseFixture()
    {
        var configuredConnection = Environment.GetEnvironmentVariable("TENANT_ADMIN_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("PLATFORM_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "Defina TENANT_ADMIN_CONNECTION_STRING ou PLATFORM_CONNECTION_STRING para os testes PostgreSQL financeiros.");
        var adminBuilder = new NpgsqlConnectionStringBuilder(configuredConnection)
        {
            Database = "postgres"
        };
        _adminConnectionString = adminBuilder.ConnectionString;
    }

    public async Task InitializeAsync()
    {
        await CreateDatabaseAsync(FirstTenantId);
        await CreateDatabaseAsync(SecondTenantId);
        await MigrateTenantAsync(FirstConnectionString);
        await MigrateTenantAsync(SecondConnectionString);
    }

    public async Task DisposeAsync()
    {
        await DropDatabaseAsync(FirstTenantId);
        await DropDatabaseAsync(SecondTenantId);
    }

    public static async Task ResetTenantAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var db = new TenantDbContext(options);
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM \"ManualTransactionAuditEvents\"; DELETE FROM \"ManualTransactions\"; DELETE FROM \"Categories\";");
    }

    public async Task<(Guid TenantId, string ConnectionString)> CreateTemporaryTenantDatabaseAsync()
    {
        var tenantId = Guid.NewGuid();
        await CreateDatabaseAsync(tenantId);
        return (tenantId, GetConnectionString(tenantId));
    }

    public Task DeleteTemporaryTenantDatabaseAsync(Guid tenantId) => DropDatabaseAsync(tenantId);

    private string GetConnectionString(Guid tenantId)
    {
        var builder = new NpgsqlConnectionStringBuilder(_adminConnectionString)
        {
            Database = $"financas_test_tenant_{tenantId:N}"
        };
        return builder.ConnectionString;
    }

    private async Task CreateDatabaseAsync(Guid tenantId)
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"financas_test_tenant_{tenantId:N}\"";
        await command.ExecuteNonQueryAsync();
    }

    private async Task DropDatabaseAsync(Guid tenantId)
    {
        await using var connection = new NpgsqlConnection(_adminConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'financas_test_tenant_{tenantId:N}' AND pid <> pg_backend_pid();";
        await command.ExecuteNonQueryAsync();
        command.CommandText = $"DROP DATABASE IF EXISTS \"financas_test_tenant_{tenantId:N}\"";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task MigrateTenantAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var db = new TenantDbContext(options);
        await db.Database.MigrateAsync();
    }
}
