using Microsoft.AspNetCore.Identity;

namespace FinancasSaaS.Platform.Infrastructure.Persistence.Entities;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }
    public bool AccessEnabled { get; set; }
}
