using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace FinancasSaaS.Finance.Infrastructure.Persistence;

public sealed class TenantDbContextFactory : IDesignTimeDbContextFactory<TenantDbContext>
{
    public TenantDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("TENANT_ADMIN_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = Environment.GetEnvironmentVariable("TenantDatabases__PlatformConnectionString");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Defina TENANT_ADMIN_CONNECTION_STRING ou TenantDatabases__PlatformConnectionString para comandos Entity Framework financeiros.");
        }

        var connection = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = "financas_tenant_design"
        };
        var options = new DbContextOptionsBuilder<TenantDbContext>()
            .UseNpgsql(connection.ConnectionString)
            .Options;

        return new TenantDbContext(options);
    }
}
