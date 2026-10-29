using System.Security.Claims;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FinancasSaaS.Web.Tests.Security;

public sealed class TenantAccessAuthorizationHandlerTests
{
    [Fact]
    public async Task UsuarioComConexaoResolvidaRecebeAutorizacao()
    {
        var userId = Guid.NewGuid();
        var handler = CreateHandler(new FixedResolver("Host=tenant"));
        var context = CreateContext(userId);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task UsuarioSemConexaoResolvidaPermaneceBloqueado()
    {
        var handler = CreateHandler(new FixedResolver(null));
        var context = CreateContext(Guid.NewGuid());

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task IdentidadeSemIdDeUsuarioNaoPodeResolverTenant()
    {
        var resolver = new FixedResolver("Host=tenant");
        var handler = CreateHandler(resolver);
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

    private static TenantAccessAuthorizationHandler CreateHandler(ITenantConnectionResolver resolver) =>
        new(resolver, new TestClock(), NullLogger<TenantAccessAuthorizationHandler>.Instance);

    private sealed class FixedResolver(string? connection) : ITenantConnectionResolver
    {
        public bool WasCalled { get; private set; }

        public Task<string?> ResolveAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return Task.FromResult(connection);
        }

        public Task<Guid?> ResolveTenantIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(connection is null ? null : userId);
    }

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }
}