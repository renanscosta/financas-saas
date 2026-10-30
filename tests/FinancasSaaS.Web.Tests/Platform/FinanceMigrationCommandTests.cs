using System.Text.Json;
using System.Diagnostics;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Finance.Infrastructure.Provisioning;
using FinancasSaaS.Web.Infrastructure;

namespace FinancasSaaS.Web.Tests.Platform;

public sealed class FinanceMigrationCommandTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task CriaEConsomeInventarioSemSegredos()
    {
        var path = Path.Combine(Path.GetTempPath(), $"finance-inventory-{Guid.NewGuid():N}.json");
        var auditPath = Path.Combine(Path.GetTempPath(), $"finance-audit-{Guid.NewGuid():N}.json");
        try
        {
            var runner = CreateRunner();
            var command = new FinanceMigrationCommand(runner);
            var output = new List<string>();
            var error = new List<string>();

            var createExitCode = await command.ExecuteAsync(
                ["--create-finance-tenant-inventory", path],
                output.Add,
                error.Add);

            Assert.Equal(0, createExitCode);
            Assert.Empty(error);
            Assert.Contains(output, message => message.Contains("0 tenants", StringComparison.Ordinal));

            var json = await File.ReadAllTextAsync(path);
            using var document = JsonDocument.Parse(json);
            Assert.Equal(2, document.RootElement.EnumerateObject().Count());
            Assert.True(document.RootElement.TryGetProperty("capturedAtUtc", out _));
            Assert.True(document.RootElement.TryGetProperty("tenantIds", out var tenantIds));
            Assert.Equal(JsonValueKind.Array, tenantIds.ValueKind);
            Assert.DoesNotContain("ConnectionString", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Password", json, StringComparison.OrdinalIgnoreCase);

            var migrateExitCode = await command.ExecuteAsync(
                ["--migrate-finance-tenants", "--tenant-inventory", path],
                output.Add,
                error.Add);

            Assert.Equal(0, migrateExitCode);
            Assert.Empty(error);

            var auditExitCode = await command.ExecuteAsync(
                ["--audit-finance-tenants", "--tenant-inventory", path, "--output", auditPath],
                output.Add,
                error.Add);

            Assert.Equal(0, auditExitCode);
            Assert.Empty(error);
            using var auditDocument = JsonDocument.Parse(await File.ReadAllTextAsync(auditPath));
            Assert.True(auditDocument.RootElement.TryGetProperty("tenants", out var auditedTenants));
            Assert.Equal(JsonValueKind.Array, auditedTenants.ValueKind);
            Assert.Empty(auditedTenants.EnumerateArray());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (File.Exists(auditPath))
            {
                File.Delete(auditPath);
            }
        }
    }

    [Fact]
    public async Task RejeitaArgumentosECaminhosInvalidosComCodigosDiferentesDeZero()
    {
        var command = new FinanceMigrationCommand(CreateRunner());
        var errors = new List<string>();
        var invalidManifestPath = Path.Combine(Path.GetTempPath(), $"missing-inventory-{Guid.NewGuid():N}.json");

        var invalidArgumentsExitCode = await command.ExecuteAsync(
            ["--create-finance-tenant-inventory"],
            writeError: errors.Add);
        var invalidPathExitCode = await command.ExecuteAsync(
            ["--create-finance-tenant-inventory", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "inventory.json")],
            writeError: errors.Add);
        var missingManifestExitCode = await command.ExecuteAsync(
            ["--migrate-finance-tenants", "--tenant-inventory", invalidManifestPath],
            writeError: errors.Add);

        Assert.Equal(2, invalidArgumentsExitCode);
        Assert.Equal(1, invalidPathExitCode);
        Assert.Equal(1, missingManifestExitCode);
        Assert.Equal(3, errors.Count);
        Assert.DoesNotContain(errors, message => message.Contains("Password", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FalhaDeAuditoriaEmiteDiagnosticoEstruturadoSemSegredos()
    {
        var path = Path.Combine(Path.GetTempPath(), $"finance-inventory-{Guid.NewGuid():N}.json");
        var tenantId = Guid.NewGuid();
        const string connectionString =
            "Host=127.0.0.1;Port=1;Database=finance-test;Username=test-user;Password=test-password;Timeout=1";
        try
        {
            await File.WriteAllTextAsync(
                path,
                JsonSerializer.Serialize(
                    new TenantFinanceInventory(DateTimeOffset.UnixEpoch, [tenantId]),
                    JsonOptions));
            var target = new TenantFinanceMigrationTarget(tenantId, "temporary", connectionString);
            var runner = new TenantFinanceMigrationRunner(new SingleTargetCatalog(target), new FixedClock());
            var command = new FinanceMigrationCommand(runner);
            var errors = new List<string>();

            var exitCode = await command.ExecuteAsync(
                ["--audit-finance-tenants", "--tenant-inventory", path, "--output", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))],
                writeError: errors.Add);

            Assert.Equal(1, exitCode);
            Assert.Equal(2, errors.Count);
            Assert.Contains(tenantId.ToString(), errors[0], StringComparison.Ordinal);
            Assert.StartsWith("Diagnóstico seguro: ", errors[1], StringComparison.Ordinal);
            var diagnosticJson = errors[1]["Diagnóstico seguro: ".Length..];
            using var diagnosticDocument = JsonDocument.Parse(diagnosticJson);
            var diagnostic = diagnosticDocument.RootElement;
            Assert.Equal("audit", diagnostic.GetProperty("operation").GetString());
            Assert.Equal(tenantId, diagnostic.GetProperty("tenantId").GetGuid());
            Assert.Equal("NpgsqlException", diagnostic.GetProperty("exceptionType").GetString());
            Assert.DoesNotContain(connectionString, errors[1], StringComparison.Ordinal);
            Assert.DoesNotContain("test-password", errors[1], StringComparison.Ordinal);
            Assert.DoesNotContain("test-user", errors[1], StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task CriacaoDeInventarioNaoSobrescreveArquivoExistente()
    {
        var path = Path.Combine(Path.GetTempPath(), $"finance-inventory-{Guid.NewGuid():N}.json");
        const string originalContent = "manifesto existente";
        try
        {
            await File.WriteAllTextAsync(path, originalContent);
            var errors = new List<string>();
            var command = new FinanceMigrationCommand(CreateRunner());

            var exitCode = await command.ExecuteAsync(
                ["--create-finance-tenant-inventory", path],
                writeError: errors.Add);

            Assert.Equal(1, exitCode);
            Assert.Single(errors);
            Assert.Equal(originalContent, await File.ReadAllTextAsync(path));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void SomenteArgumentosDosComandosFinanceirosSaoInterceptadosAntesDoServidor()
    {
        Assert.True(FinanceMigrationCommand.IsCommand(["--create-finance-tenant-inventory", "inventory.json"]));
        Assert.True(FinanceMigrationCommand.IsCommand(["--migrate-finance-tenants", "--tenant-inventory", "inventory.json"]));
        Assert.True(FinanceMigrationCommand.IsCommand(["--audit-finance-tenants", "--tenant-inventory", "inventory.json", "--output", "audit.json"]));
        Assert.False(FinanceMigrationCommand.IsCommand([]));
        Assert.False(FinanceMigrationCommand.IsCommand(["--urls", "http://localhost:5000"]));
    }

    [Fact]
    public async Task ComandoOperacionalRetornaSemIniciarServidorWeb()
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("dotnet")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.StartInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
        process.StartInfo.ArgumentList.Add("--create-finance-tenant-inventory");

        Assert.True(process.Start());
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(15));

        Assert.Equal(2, process.ExitCode);
        Assert.Empty(await standardOutput);
        Assert.Contains("Argumentos inválidos", await standardError, StringComparison.Ordinal);
    }

    private static TenantFinanceMigrationRunner CreateRunner() =>
        new(new EmptyCatalog(), new FixedClock());

    private sealed class FixedClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UnixEpoch;
    }

    private sealed class SingleTargetCatalog(TenantFinanceMigrationTarget target) : ITenantProvisioningCatalog
    {
        public Task<TenantProvisioningTarget?> GetAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantProvisioningTarget?>(null);

        public Task<IReadOnlyList<TenantFinanceMigrationTarget>> ListReadyAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TenantFinanceMigrationTarget>>([target]);
    }

    private sealed class EmptyCatalog : ITenantProvisioningCatalog
    {
        public Task<TenantProvisioningTarget?> GetAsync(
            Guid tenantId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TenantProvisioningTarget?>(null);

        public Task<IReadOnlyList<TenantFinanceMigrationTarget>> ListReadyAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TenantFinanceMigrationTarget>>([]);
    }

}
