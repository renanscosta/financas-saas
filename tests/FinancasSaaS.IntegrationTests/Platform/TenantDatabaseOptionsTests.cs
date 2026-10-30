using FinancasSaaS.Platform.Infrastructure.Configuration;
using Npgsql;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class TenantDatabaseOptionsTests
{
    [Fact]
    public void DerivaConexaoDoBancoDoTenantDaConexaoDaPlataforma()
    {
        var options = new TenantDatabaseOptions
        {
            PlatformConnectionString =
                "Host=localhost;Port=5432;Database=financas_platform;Username=financas;Password=local-secret"
        };

        var connectionString = options.GetConnectionString("financas_tenant_123");
        var builder = new NpgsqlConnectionStringBuilder(connectionString);

        Assert.Equal("financas_tenant_123", builder.Database);
        Assert.Equal("localhost", builder.Host);
        Assert.Equal("financas", builder.Username);
        Assert.Equal("local-secret", builder.Password);
    }

    [Fact]
    public void SobrescritaEspecificaDoTenantTemPrecedenciaSobreConexaoBase()
    {
        const string specificConnection =
            "Host=tenant-db;Database=financas_tenant_123;Username=tenant-user;Password=tenant-secret";
        var options = new TenantDatabaseOptions
        {
            PlatformConnectionString = "Host=localhost;Database=financas_platform;Username=financas",
            Connections = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["financas_tenant_123"] = specificConnection
            }
        };

        Assert.Equal(specificConnection, options.GetConnectionString("financas_tenant_123"));
    }

    [Fact]
    public void SemConexaoBaseOuSobrescritaRetornaNulo()
    {
        var options = new TenantDatabaseOptions();

        Assert.Null(options.GetConnectionString("financas_tenant_123"));
    }
}
