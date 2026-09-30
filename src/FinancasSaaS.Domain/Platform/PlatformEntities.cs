namespace FinancasSaaS.Domain.Platform;

public sealed class Customer
{
    public Customer(Guid id, string email, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("O cliente precisa de um identificador.", nameof(id));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("O e-mail do cliente é obrigatório.", nameof(email));

        Id = id;
        Email = email.Trim();
        CreatedAt = createdAt;
        Status = CustomerStatus.Active;
    }

    public Guid Id { get; }
    public string Email { get; }
    public DateTimeOffset CreatedAt { get; }
    public CustomerStatus Status { get; private set; }

    public void Disable()
    {
        Status = CustomerStatus.Disabled;
    }
}

public sealed class Tenant
{
    public Tenant(Guid id, Guid customerId, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("O tenant precisa de um identificador.", nameof(id));
        if (customerId == Guid.Empty) throw new ArgumentException("O cliente é obrigatório.", nameof(customerId));

        Id = id;
        CustomerId = customerId;
        CreatedAt = createdAt;
        ProvisioningStatus = ProvisioningStatus.Pending;
    }

    public Guid Id { get; }
    public Guid CustomerId { get; }
    public DateTimeOffset CreatedAt { get; }
    public ProvisioningStatus ProvisioningStatus { get; private set; }

    public void StartProvisioning()
    {
        if (ProvisioningStatus == ProvisioningStatus.Ready)
        {
            throw new InvalidOperationException("Um tenant pronto não pode iniciar novo provisionamento.");
        }

        ProvisioningStatus = ProvisioningStatus.InProgress;
    }

    public void MarkReady() => ProvisioningStatus = ProvisioningStatus.Ready;
    public void MarkFailed() => ProvisioningStatus = ProvisioningStatus.Failed;
}
