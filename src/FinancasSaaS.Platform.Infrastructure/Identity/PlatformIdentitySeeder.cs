using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace FinancasSaaS.Platform.Infrastructure.Identity;

public sealed class PlatformIdentitySeeder(
    RoleManager<IdentityRole<Guid>> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<IdentitySeedOptions> options)
{
    private const string ReadOnlyOperatorRole = "platform.readonly";
    private const string AdministratorRole = "platform.admin";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var roleName in new[] { ReadOnlyOperatorRole, AdministratorRole })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                EnsureSucceeded(result, $"Não foi possível criar a role {roleName}.");
            }
        }

        if (!options.Value.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Value.AdministratorEmail) || string.IsNullOrWhiteSpace(options.Value.AdministratorPassword))
        {
            throw new InvalidOperationException("IdentitySeed requer e-mail e senha do administrador quando está habilitado.");
        }

        var administrator = await userManager.FindByEmailAsync(options.Value.AdministratorEmail);
        if (administrator is null)
        {
            administrator = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = options.Value.AdministratorEmail,
                Email = options.Value.AdministratorEmail,
                EmailConfirmed = true,
                AccessEnabled = true
            };
            var createResult = await userManager.CreateAsync(administrator, options.Value.AdministratorPassword);
            EnsureSucceeded(createResult, "Não foi possível criar o usuário administrador.");
        }

        if (!await userManager.IsInRoleAsync(administrator, AdministratorRole))
        {
            var roleResult = await userManager.AddToRoleAsync(administrator, AdministratorRole);
            EnsureSucceeded(roleResult, "Não foi possível associar a role de administrador.");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"{message} {string.Join("; ", result.Errors.Select(error => error.Code))}");
        }
    }
}