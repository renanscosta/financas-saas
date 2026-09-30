using FinancasSaaS.Application.Platform.RegisterCustomer;
using FinancasSaaS.Application.Abstractions;
using FinancasSaaS.Platform.Infrastructure.Persistence;
using FinancasSaaS.Platform.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancasSaaS.Platform.Infrastructure;

public sealed class PlatformCustomerRegistrationStore(
    PlatformDbContext dbContext,
    ISystemClock clock,
    IAuditWriter auditWriter) : ICustomerRegistrationStore
{
    public async Task<RegisterCustomerResult?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var customer = await dbContext.Customers
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Email == email, cancellationToken);

        return customer is null
            ? null
            : new RegisterCustomerResult(customer.Id, customer.TenantId, true);
    }

    public async Task<RegisterCustomerResult> CreateAsync(string email, string actorId, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var customerId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = clock.UtcNow;
        var databaseName = $"financas_tenant_{tenantId:N}";

        dbContext.Customers.Add(new CustomerEntity
        {
            Id = customerId,
            Email = email,
            TenantId = tenantId,
            Status = Domain.Platform.CustomerStatus.Active,
            CreatedAt = now
        });
        dbContext.Tenants.Add(new TenantEntity
        {
            Id = tenantId,
            CustomerId = customerId,
            DatabaseName = databaseName,
            ConnectionSecretReference = databaseName,
            Status = Domain.Platform.CustomerStatus.Active,
            ProvisioningStatus = Domain.Platform.ProvisioningStatus.Pending,
            CreatedAt = now
        });
        dbContext.Provisionings.Add(new ProvisioningEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Status = Domain.Platform.ProvisioningStatus.Pending,
            Attempt = 0,
            LastAttemptAt = now
        });
        dbContext.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = email,
            NormalizedUserName = email,
            Email = email,
            NormalizedEmail = email,
            TenantId = tenantId,
            AccessEnabled = false,
            EmailConfirmed = false
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await auditWriter.WriteAsync("customer.registered", tenantId, actorId, null, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new RegisterCustomerResult(customerId, tenantId, false);
        }
        catch (DbUpdateException exception)
        {
            dbContext.ChangeTracker.Clear();
            var existing = await FindByEmailAsync(email, cancellationToken);
            if (existing is null)
            {
                throw new InvalidOperationException("O cadastro concorrente falhou sem um cliente vencedor.", exception);
            }

            return existing with { AlreadyExists = true };
        }
    }
}
