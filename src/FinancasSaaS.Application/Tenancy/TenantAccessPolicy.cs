namespace FinancasSaaS.Application.Tenancy;

public sealed record TenantAccessDecision(bool Allowed, string? ConnectionString)
{
    public static TenantAccessDecision Denied() => new(false, null);
    public static TenantAccessDecision Granted(string connectionString) => new(true, connectionString);
}
