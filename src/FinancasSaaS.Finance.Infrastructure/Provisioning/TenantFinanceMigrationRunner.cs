using FinancasSaaS.Application.Abstractions;
using DomainCategory = FinancasSaaS.Domain.Finance.Category;
using FinancasSaaS.Finance.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Finance.Infrastructure.Provisioning;

public sealed record TenantFinanceInventory(DateTimeOffset CapturedAtUtc, IReadOnlyList<Guid> TenantIds);
public sealed record TenantFinanceAudit(DateTimeOffset CapturedAtUtc, IReadOnlyList<TenantFinanceTenantAudit> Tenants);
public sealed record TenantFinanceTenantAudit(
    Guid TenantId,
    IReadOnlyList<TenantFinanceMigrationStatus> FinanceMigrations,
    IReadOnlyList<TenantDefaultCategoryAudit> DefaultCategories);
public sealed record TenantFinanceMigrationStatus(string MigrationId, bool Applied);
public sealed record TenantDefaultCategoryAudit(string Name, string State);

public sealed class TenantFinanceMigrationException(string message) : Exception(message);

public sealed class TenantFinanceMigrationRunner(
    ITenantProvisioningCatalog catalog,
    ISystemClock clock)
{
    private const string FinanceMigrationId = "20261028032258_ManualTransactionsAndNormalizedCategories";
    private const string ReactivateDefaultsMigrationId = "20261029181614_ReactivateExistingDefaultCategories";
    private static readonly string[] DefaultCategories = ["Lanches", "Mercado", "Pessoal"];
    private static readonly string[] FinanceMigrationIds = [FinanceMigrationId, ReactivateDefaultsMigrationId];

    public async Task<TenantFinanceInventory> CreateInventoryAsync(CancellationToken cancellationToken = default)
    {
        var targets = await catalog.ListReadyAsync(cancellationToken);
        return new TenantFinanceInventory(
            clock.UtcNow.ToUniversalTime(),
            targets.Select(item => item.TenantId).Order().ToArray());
    }

    public async Task<TenantFinanceAudit> AuditAsync(
        TenantFinanceInventory inventory,
        CancellationToken cancellationToken = default)
    {
        ValidateInventory(inventory);
        var expectedIds = inventory.TenantIds.Order().ToArray();
        var targets = await catalog.ListReadyAsync(cancellationToken);
        EnsureExactTenantSet(expectedIds, targets);

        var tenantAudits = new List<TenantFinanceTenantAudit>(targets.Count);
        foreach (var target in targets.OrderBy(item => item.TenantId))
        {
            if (string.IsNullOrWhiteSpace(target.ConnectionString))
            {
                throw new TenantFinanceMigrationException($"Tenant {target.TenantId} não possui conexão configurada.");
            }

            try
            {
                var options = new DbContextOptionsBuilder<TenantDbContext>()
                    .UseNpgsql(target.ConnectionString)
                    .Options;
                await using var db = new TenantDbContext(options);
                var appliedMigrations = await db.Database.GetAppliedMigrationsAsync(cancellationToken);
                var categories = await db.Categories
                    .AsNoTracking()
                    .Select(item => new { item.Name, item.IsActive })
                    .ToListAsync(cancellationToken);
                var defaults = DefaultCategories
                    .Select(name =>
                    {
                        var matches = categories
                            .Where(item => DomainCategory.NormalizeName(item.Name) == DomainCategory.NormalizeName(name))
                            .ToArray();
                        var state = matches.Length switch
                        {
                            0 => "missing",
                            > 1 => "conflict",
                            _ => matches[0].IsActive ? "active" : "inactive"
                        };
                        return new TenantDefaultCategoryAudit(name, state);
                    })
                    .ToArray();

                tenantAudits.Add(new TenantFinanceTenantAudit(
                    target.TenantId,
                    FinanceMigrationIds
                        .Select(id => new TenantFinanceMigrationStatus(id, appliedMigrations.Contains(id)))
                        .ToArray(),
                    defaults));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new TenantFinanceMigrationException(
                    $"Falha ao auditar o tenant {target.TenantId}. O rollout deve permanecer bloqueado.");
            }
        }

        var currentTargets = await catalog.ListReadyAsync(cancellationToken);
        EnsureExactTenantSet(expectedIds, currentTargets);
        return new TenantFinanceAudit(clock.UtcNow.ToUniversalTime(), tenantAudits);
    }

    public async Task<IReadOnlyList<Guid>> MigrateAsync(
        TenantFinanceInventory inventory,
        Action<Guid, string>? reportProgress = null,
        CancellationToken cancellationToken = default)
    {
        ValidateInventory(inventory);
        var expectedIds = inventory.TenantIds.Order().ToArray();
        var targets = await catalog.ListReadyAsync(cancellationToken);
        EnsureExactTenantSet(expectedIds, targets);

        foreach (var target in targets.OrderBy(item => item.TenantId))
        {
            if (string.IsNullOrWhiteSpace(target.ConnectionString))
            {
                throw new TenantFinanceMigrationException($"Tenant {target.TenantId} não possui conexão configurada.");
            }

            reportProgress?.Invoke(target.TenantId, "iniciado");
            try
            {
                var options = new DbContextOptionsBuilder<TenantDbContext>()
                    .UseNpgsql(target.ConnectionString)
                    .Options;
                await using var db = new TenantDbContext(options);
                await db.Database.MigrateAsync(cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                reportProgress?.Invoke(target.TenantId, "falhou");
                throw new TenantFinanceMigrationException(
                    $"Falha ao atualizar o tenant {target.TenantId}. O rollout deve permanecer bloqueado.");
            }

            reportProgress?.Invoke(target.TenantId, "concluído");
        }

        var currentTargets = await catalog.ListReadyAsync(cancellationToken);
        EnsureExactTenantSet(expectedIds, currentTargets);
        return expectedIds;
    }

    private static void ValidateInventory(TenantFinanceInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        if (inventory.CapturedAtUtc == default ||
            inventory.CapturedAtUtc.Offset != TimeSpan.Zero ||
            inventory.TenantIds is null ||
            inventory.TenantIds.Any(id => id == Guid.Empty) ||
            inventory.TenantIds.Distinct().Count() != inventory.TenantIds.Count)
        {
            throw new TenantFinanceMigrationException("O inventário financeiro é inválido.");
        }
    }

    private static void EnsureExactTenantSet(
        IReadOnlyCollection<Guid> expectedIds,
        IReadOnlyCollection<TenantFinanceMigrationTarget> targets)
    {
        var currentIds = targets.Select(item => item.TenantId).Order().ToArray();
        if (!expectedIds.SequenceEqual(currentIds))
        {
            throw new TenantFinanceMigrationException("O conjunto de tenants prontos diverge do inventário financeiro.");
        }
    }

}
