using System.Text.Json;
using FinancasSaaS.Finance.Infrastructure.Provisioning;

namespace FinancasSaaS.Web.Infrastructure;

public sealed class FinanceMigrationCommand(TenantFinanceMigrationRunner runner)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public static bool IsCommand(IReadOnlyList<string> args) =>
        args.Count > 0 &&
        args[0] is "--create-finance-tenant-inventory" or "--migrate-finance-tenants" or "--audit-finance-tenants";

    public async Task<int> ExecuteAsync(
        IReadOnlyList<string> args,
        Action<string>? writeOutput = null,
        Action<string>? writeError = null)
    {
        writeOutput ??= Console.WriteLine;
        writeError ??= Console.Error.WriteLine;

        try
        {
            if (args.Count == 2 && args[0] == "--create-finance-tenant-inventory")
            {
                var inventory = await runner.CreateInventoryAsync();
                await using var file = new FileStream(args[1], FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await JsonSerializer.SerializeAsync(file, inventory, JsonOptions);
                writeOutput($"Inventário financeiro criado com {inventory.TenantIds.Count} tenants.");
                return 0;
            }

            if (args.Count == 3 &&
                args[0] == "--migrate-finance-tenants" &&
                args[1] == "--tenant-inventory")
            {
                var json = await File.ReadAllTextAsync(args[2]);
                var inventory = JsonSerializer.Deserialize<TenantFinanceInventory>(json, JsonOptions)
                    ?? throw new TenantFinanceMigrationException("O inventário financeiro está vazio ou inválido.");
                await runner.MigrateAsync(
                    inventory,
                    (tenantId, status) => writeOutput($"Tenant {tenantId}: {status}."));
                return 0;
            }

            if (args.Count == 5 &&
                args[0] == "--audit-finance-tenants" &&
                args[1] == "--tenant-inventory" &&
                args[3] == "--output")
            {
                var json = await File.ReadAllTextAsync(args[2]);
                var inventory = JsonSerializer.Deserialize<TenantFinanceInventory>(json, JsonOptions)
                    ?? throw new TenantFinanceMigrationException("O inventário financeiro está vazio ou inválido.");
                var audit = await runner.AuditAsync(inventory);
                await using var file = new FileStream(args[4], FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await JsonSerializer.SerializeAsync(file, audit, JsonOptions);
                writeOutput($"Auditoria financeira criada para {audit.Tenants.Count} tenants.");
                return 0;
            }

            writeError("Argumentos inválidos para o comando financeiro.");
            return 2;
        }
        catch (TenantFinanceMigrationException exception)
        {
            writeError(exception.Message);
            if (exception.Diagnostic is { } diagnostic)
            {
                writeError($"Diagnóstico seguro: {JsonSerializer.Serialize(diagnostic, JsonOptions)}");
            }

            return 1;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            writeError("Falha operacional no comando financeiro. Nenhum segredo foi exibido; mantenha o rollout bloqueado.");
            return 1;
        }
    }
}
