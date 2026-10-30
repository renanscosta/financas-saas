using FinancasSaaS.Platform.Infrastructure.Identity;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace FinancasSaaS.IntegrationTests.Platform;

public sealed class IdentitySeederIntegrationTests
{
    [Fact]
    public async Task SeederCriaSomenteRolesPrevistasEAdministradorDeFormaIdempotente()
    {
        var connection = FinancasSaaS.IntegrationTests.Infrastructure.IntegrationTestConnections.Platform;
        var email = $"admin-{Guid.NewGuid():N}@example.com";
        var services = new ServiceCollection();
        services.AddDbContext<PlatformDbContext>(options => options.UseNpgsql(connection));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<PlatformDbContext>();
        services.AddSingleton<IOptions<IdentitySeedOptions>>(Options.Create(new IdentitySeedOptions
        {
            Enabled = true,
            AdministratorEmail = email,
            AdministratorPassword = "StrongPassword123!"
        }));
        services.AddScoped<PlatformIdentitySeeder>();

        await using var provider = services.BuildServiceProvider();
        await using (var migrationScope = provider.CreateAsyncScope())
        {
            await migrationScope.ServiceProvider.GetRequiredService<PlatformDbContext>().Database.MigrateAsync();
        }

        await using (var seedScope = provider.CreateAsyncScope())
        {
            await seedScope.ServiceProvider.GetRequiredService<PlatformIdentitySeeder>().SeedAsync();
        }

        await using (var secondSeedScope = provider.CreateAsyncScope())
        {
            await secondSeedScope.ServiceProvider.GetRequiredService<PlatformIdentitySeeder>().SeedAsync();
        }

        await using var verificationScope = provider.CreateAsyncScope();
        var roleManager = verificationScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = verificationScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);

        Assert.True(await roleManager.RoleExistsAsync("platform.readonly"));
        Assert.True(await roleManager.RoleExistsAsync("platform.admin"));
        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user, "platform.admin"));
        Assert.Equal(1, await userManager.Users.CountAsync(item => item.Email == email));
    }
}