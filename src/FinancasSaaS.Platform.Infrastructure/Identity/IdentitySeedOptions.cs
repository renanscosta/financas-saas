namespace FinancasSaaS.Platform.Infrastructure.Identity;

public sealed class IdentitySeedOptions
{
    public bool Enabled { get; set; }
    public string? AdministratorEmail { get; set; }
    public string? AdministratorPassword { get; set; }
}