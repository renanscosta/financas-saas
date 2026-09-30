using System.Security.Claims;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace FinancasSaaS.Web.Tests.Security;

public sealed class TenantAccessAuthorizationHandlerTests
{
    [Fact]
    public async Task UsuarioComConexaoResolvidaRecebeAutorizacao()
    {
        var userId = Guid.NewGuid();
        var handler = new TenantAccessAuthorizationHandler(new FixedResolver("Host=tenant"));
        var context = CreateContext(userId);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task UsuarioSemConexaoResolvidaPermaneceBloqueado()
    {
        var handler = new TenantAccessAuthorizationHandler(new FixedResolver(null));
        var context = CreateContext(Guid.NewGuid());

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task IdentidadeSemIdDeUsuarioNaoPodeResolverTenant()
    {
        var resolver = new FixedResolver("Host=tenant");
        var handler = new TenantAccessAuthorizationHandler(resolver);
        var context = new AuthorizationHandlerContext(
            [new TenantAccessRequirement()],
            new ClaimsPrincipal(new ClaimsIdentity()),
            null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.False(resolver.WasCalled);
    }

    private static AuthorizationHandlerContext CreateContext(Guid userId) => new(
        [new TenantAccessRequirement()],
        new ClaimsPrincipal(new ClaimsIdentity(
        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            "test")),
        null);

    private sealed class FixedResolver(string? connection) : ITenantConnectionResolver
    {
        public bool WasCalled { get; private set; }

        public Task<string?> ResolveAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(connection);
        }
    }
}