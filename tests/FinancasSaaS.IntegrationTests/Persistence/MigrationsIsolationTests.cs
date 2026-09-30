using FinancasSaaS.Finance.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Persistence;

public sealed class MigrationsIsolationTests
{
    [Fact]
    public void ContextosUsamAssembliesDeMigrationDistintos()
    {
        Assert.NotEqual(
            typeof(PlatformDbContext).Assembly,
            typeof(TenantDbContext).Assembly);
    }
}
