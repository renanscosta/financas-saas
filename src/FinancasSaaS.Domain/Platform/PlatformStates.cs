namespace FinancasSaaS.Domain.Platform;

public enum CustomerStatus
{
    Active = 1,
    Disabled = 2
}

public enum ProvisioningStatus
{
    Pending = 1,
    InProgress = 2,
    Ready = 3,
    Failed = 4
}

public enum RegistrationStatus
{
    Pending = 1,
    Completed = 2
}

public enum AccessStatus
{
    Blocked = 1,
    Enabled = 2
}
