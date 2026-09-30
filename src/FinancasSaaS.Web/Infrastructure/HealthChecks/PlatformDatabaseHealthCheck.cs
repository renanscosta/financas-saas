using System.Data.Common;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FinancasSaaS.Web.Infrastructure.HealthChecks;

public sealed class PlatformDatabaseHealthCheck(PlatformDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("PostgreSQL da plataforma indisponível.");
        }
        catch (Exception exception) when (exception is DbException or OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL da plataforma indisponível.");
        }
    }
}