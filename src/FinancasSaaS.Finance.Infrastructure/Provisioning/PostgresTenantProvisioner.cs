using FinancasSaaS.Application.Abstractions;
using DomainCategory = FinancasSaaS.Domain.Finance.Category;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Finance.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FinancasSaaS.Finance.Infrastructure.Provisioning;

public sealed class PostgresTenantProvisioner(
    ITenantProvisioningCatalog catalog,
    ITenantProvisioningStateStore stateStore,
    ISystemClock clock) : ITenantProvisioner
{
    private static readonly string[] DefaultCategories = ["Lanches", "Mercado", "Pessoal"];

    public async Task ProvisionAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!await stateStore.BeginAsync(tenantId, clock.UtcNow, cancellationToken))
        {
            return;
        }

        try
        {
            var target = await catalog.GetAsync(tenantId, cancellationToken)
                ?? throw new InvalidOperationException("Tenant não encontrado ou sem conexão configurada.");
            await EnsureDatabaseExistsAsync(target.ConnectionString, cancellationToken);
            var tenantOptions = new DbContextOptionsBuilder<TenantDbContext>()
                .UseNpgsql(target.ConnectionString)
                .Options;
            await using var tenantDb = new TenantDbContext(tenantOptions);
            await tenantDb.Database.MigrateAsync(cancellationToken);

            foreach (var categoryName in DefaultCategories)
            {
                var normalizedName = DomainCategory.NormalizeName(categoryName);
                var existing = await tenantDb.Categories
                    .SingleOrDefaultAsync(item => item.NormalizedName == normalizedName, cancellationToken);
                if (existing is null)
                {
                    tenantDb.Categories.Add(new Category { Id = Guid.NewGuid(), Name = categoryName, IsActive = true });
                }
                else if (!existing.IsActive)
                {
                    existing.IsActive = true;
                }
            }

            await tenantDb.SaveChangesAsync(cancellationToken);
            await stateStore.MarkReadyAsync(tenantId, clock.UtcNow, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await stateStore.MarkFailedAsync(tenantId, clock.UtcNow, "Falha técnica ao preparar o banco do tenant.", cancellationToken);
            throw;
        }
    }

    private static async Task EnsureDatabaseExistsAsync(string connectionString, CancellationToken cancellationToken)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        var databaseName = builder.Database ?? throw new InvalidOperationException("A conexão do tenant não informa o banco.");
        builder.Database = "postgres";
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM pg_database WHERE datname = @database";
        command.Parameters.AddWithValue("database", databaseName);
        if (await command.ExecuteScalarAsync(cancellationToken) is not null)
        {
            return;
        }

        command.Parameters.Clear();
        command.CommandText = $"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"")}\"";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
