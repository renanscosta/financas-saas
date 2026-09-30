using System.Security.Claims;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace FinancasSaaS.Web.Tests.Security;

public sealed class ProvisioningRetryAuthorizationTests
{
    [Fact]
    public async Task OperadorSomenteLeituraNaoPodeRetentarProvisionamento()
    {
        var provisioner = new RecordingProvisioner();
        var service = new ProvisioningRetryService(new FixedAuthorizationService(false), provisioner);

        var result = await service.ExecuteAsync(new ClaimsPrincipal(new ClaimsIdentity("readonly")), Guid.NewGuid());

        Assert.False(result);
        Assert.False(provisioner.Called);
    }

    [Fact]
    public async Task AdministradorPodeRetentarProvisionamento()
    {
        var provisioner = new RecordingProvisioner();
        var service = new ProvisioningRetryService(new FixedAuthorizationService(true), provisioner);

        var result = await service.ExecuteAsync(new ClaimsPrincipal(new ClaimsIdentity("admin")), Guid.NewGuid());

        Assert.True(result);
        Assert.True(provisioner.Called);
    }

    private sealed class FixedAuthorizationService(bool allowed) : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(allowed ? AuthorizationResult.Success() : AuthorizationResult.Failed());

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(allowed ? AuthorizationResult.Success() : AuthorizationResult.Failed());
    }

    private sealed class RecordingProvisioner : ITenantProvisioner
    {
        public bool Called { get; private set; }

        public Task ProvisionAsync(Guid tenantId, CancellationToken cancellationToken = default)
        {
            Called = true;
            return Task.CompletedTask;
        }
    }
}