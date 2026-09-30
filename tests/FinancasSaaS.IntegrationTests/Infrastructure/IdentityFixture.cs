namespace FinancasSaaS.IntegrationTests.Infrastructure;

public sealed record TestIdentity(Guid UserId, Guid TenantId, bool IsOperator);

public sealed class IdentityFixture
{
    public TestIdentity Operator { get; } = new(Guid.NewGuid(), Guid.Empty, true);

    public IReadOnlyList<TestIdentity> TenantUsers { get; } =
    [
        new TestIdentity(Guid.NewGuid(), Guid.NewGuid(), false),
        new TestIdentity(Guid.NewGuid(), Guid.NewGuid(), false)
    ];
}
