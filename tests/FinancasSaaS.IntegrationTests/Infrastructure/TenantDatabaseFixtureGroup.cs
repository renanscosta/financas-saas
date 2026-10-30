namespace FinancasSaaS.IntegrationTests.Infrastructure;

[CollectionDefinition("Tenant database integration", DisableParallelization = true)]
public sealed class TenantDatabaseFixtureGroup : ICollectionFixture<TenantDatabaseFixture>
{
}
